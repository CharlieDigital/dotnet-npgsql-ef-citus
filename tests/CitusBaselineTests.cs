using Npgsql;

public class CitusBaselineTests(CitusSqlFixture citus) : IClassFixture<CitusSqlFixture>
{
    [Fact]
    public void Can_Perform_Baseline_Connection()
    {
        using var connection = citus.CreateConnection();
        connection.Open();

        Assert.Equal(System.Data.ConnectionState.Open, connection.State);
    }

    [Fact]
    public void Can_Create_Single_Distributed_Table()
    {
        using var connection = citus.CreateConnection();
        connection.Open();
        using var tx = connection.BeginTransaction();

        using var command = connection.CreateCommand();
        command.CommandText =
            @"
            CREATE TABLE district (
                id UUID,
                name TEXT NOT NULL
            );

            SELECT create_distributed_table('district', 'id');
        ";

        var result = command.ExecuteNonQuery();
    }

    [Fact]
    public void Primary_Key_Without_Distribution_Key_Fails()
    {
        using var connection = citus.CreateConnection();
        connection.Open();
        using var tx = connection.BeginTransaction();

        using var command = connection.CreateCommand();
        command.CommandText =
            @"
            CREATE TABLE district (
                id UUID PRIMARY KEY,
                name TEXT NOT NULL
            );

            CREATE TABLE schools (
                -- 👇 Will fail because PK does not include partition key
                id UUID PRIMARY KEY,
                name TEXT NOT NULL,
                district_id UUID
            );

            SELECT create_distributed_table('district', 'id');
            SELECT create_distributed_table('schools', 'district_id');
        ";

        // ❌ Distributed relations cannot have UNIQUE, EXCLUDE, or PRIMARY KEY
        // constraints that do not include the partition column (with an equality
        // operator if EXCLUDE)
        Assert.Throws<PostgresException>(() => command.ExecuteNonQuery());
    }

    [Fact]
    public void Primary_Key_With_Distribution_Key_Succeeds()
    {
        using var connection = citus.CreateConnection();
        connection.Open();
        using var tx = connection.BeginTransaction();

        using var command = connection.CreateCommand();
        command.CommandText =
            @"
            CREATE TABLE district (
                id UUID PRIMARY KEY,
                name TEXT NOT NULL
            );

            CREATE TABLE schools (
                id UUID,
                name TEXT NOT NULL,
                district_id UUID REFERENCES district(id)
            );

            -- 👇 MUST include the partition column
            ALTER TABLE schools
                ADD PRIMARY KEY (district_id, id);

            SELECT create_distributed_table('district', 'id');
            SELECT create_distributed_table('schools', 'district_id');
        ";
    }

    [Fact]
    public void Table_With_Reference_Cannot_Be_Distributed()
    {
        using var connection = citus.CreateConnection();
        connection.Open();
        using var tx = connection.BeginTransaction();

        using var command = connection.CreateCommand();
        command.CommandText =
            @"
            -- Tables cannot be created with constraints (including primary key)
            CREATE TABLE district (
                id UUID PRIMARY KEY,
                name TEXT NOT NULL
            );

            CREATE TABLE schools (
                id UUID,
                name TEXT NOT NULL,
                district_id UUID REFERENCES district(id)
            );

            CREATE TABLE student (
                id UUID,
                name TEXT NOT NULL,
                district_id UUID REFERENCES district(id),
                -- 👇 Fails because we can't add the PK to schools
                school_id UUID REFERENCES schools(id)
            );

            -- Add primary keys including distribution keys
            ALTER TABLE schools
                ADD PRIMARY KEY (district_id, id);
            ALTER TABLE student
                ADD PRIMARY KEY (district_id, id);

            -- Mark tables as distributed
            SELECT create_distributed_table('district', 'id');
            SELECT create_distributed_table('schools', 'district_id');
            SELECT create_distributed_table('student', 'district_id');
        ";

        // ❌ 42830: there is no unique constraint matching given keys for referenced
        // table "schools"
        Assert.Throws<PostgresException>(() => command.ExecuteNonQuery());
    }

    [Fact]
    public void Table_Can_Be_Distributed_With_Reference_Added_After()
    {
        using var connection = citus.CreateConnection();
        connection.Open();
        using var tx = connection.BeginTransaction();

        using var command = connection.CreateCommand();
        command.CommandText =
            @"
            -- Tables cannot be created with constraints (including primary key)
            CREATE TABLE district (
                id UUID PRIMARY KEY,
                name TEXT NOT NULL
            );

            CREATE TABLE schools (
                id UUID,
                name TEXT NOT NULL,
                district_id UUID REFERENCES district(id)
            );

            CREATE TABLE student (
                id UUID,
                name TEXT NOT NULL,
                district_id UUID REFERENCES district(id),
                -- ✅ Add the reference AFTER distribution
                school_id UUID
            );

            -- Add primary keys including distribution keys
            ALTER TABLE schools
                ADD PRIMARY KEY (district_id, id);
            ALTER TABLE student
                ADD PRIMARY KEY (district_id, id);

            -- 👇 Add the reference constraint
            ALTER TABLE student
                ADD CONSTRAINT fk_school
                FOREIGN KEY (school_id, district_id)
                REFERENCES schools(id, district_id);

            -- Mark tables as distributed
            SELECT create_distributed_table('district', 'id');
            SELECT create_distributed_table('schools', 'district_id');
            SELECT create_distributed_table('student', 'district_id');
        ";

        command.ExecuteNonQuery();
    }
}
