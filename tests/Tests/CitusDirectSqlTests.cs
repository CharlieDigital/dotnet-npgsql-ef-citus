using System.Transactions;
using Npgsql;

public class CitusDirectSqlTests(CitusSqlFixture citus) : IClassFixture<CitusSqlFixture>
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
                -- ❌ Fails because we can't add the reference here since the PK isn't defined yet
                school_id UUID REFERENCES schools(district_id, id)
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

            -- 👇 Add the reference constraint with BOTH keys
            ALTER TABLE student
                ADD CONSTRAINT fk_school
                FOREIGN KEY (school_id, district_id)
                REFERENCES schools(id, district_id);
                -- 👆 The order of the keys is important here.

            -- Mark tables as distributed
            SELECT create_distributed_table('district', 'id');
            SELECT create_distributed_table('schools', 'district_id');
            SELECT create_distributed_table('student', 'district_id');
        ";

        command.ExecuteNonQuery();
    }

    [Fact]
    public void Table_Can_Be_Distributed_With_Reference_In_Wrong_Order_Causes_Error()
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

            -- 👇 Add the reference constraint with BOTH keys
            ALTER TABLE student
                ADD CONSTRAINT fk_school
                FOREIGN KEY (school_id, district_id)
                REFERENCES schools(district_id, id);
                -- ❌ The order of the keys is important here; the wrong order from
                -- line above is an error.

            -- Mark tables as distributed
            SELECT create_distributed_table('district', 'id');
            SELECT create_distributed_table('schools', 'district_id');
            SELECT create_distributed_table('student', 'district_id');
        ";

        // ❌ Foreign keys are supported in two cases, either in between two
        // colocated tables including partition column in the same ordinal in the
        // both tables or from distributed to reference tables
        Assert.Throws<PostgresException>(() => command.ExecuteNonQuery());
    }

    [Fact]
    public void Table_With_Reference_Using_Composite_Primary_Key_Works()
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
                district_id UUID REFERENCES district(id),
                PRIMARY KEY (district_id, id)
            );

            CREATE TABLE student (
                id UUID,
                name TEXT NOT NULL,
                district_id UUID REFERENCES district(id),
                school_id UUID,
                PRIMARY KEY (district_id, id),
                -- ✅ Works because the table has a composite primary key
                FOREIGN KEY (district_id, school_id) REFERENCES schools(district_id, id)
            );

            -- Mark tables as distributed
            SELECT create_distributed_table('district', 'id');
            SELECT create_distributed_table('schools', 'district_id');
            SELECT create_distributed_table('student', 'district_id');
        ";

        // This test case works because the FK includes the distribution key
        command.ExecuteNonQuery();
    }

    [Fact]
    public void Can_Create_Distributed_Table_With_Reference_Table()
    {
        using var connection = citus.CreateConnection();
        connection.Open();
        using var tx = connection.BeginTransaction();

        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE district (
                id UUID PRIMARY KEY,
                name TEXT NOT NULL
            );

            CREATE TABLE school_type (
                id uuid NOT NULL,
                name text NOT NULL,
                CONSTRAINT pk_school_type PRIMARY KEY (id)
            );
            """;

        command.ExecuteNonQuery();

        command.CommandText = """
            -- Mark tables as referenced and distributed
            SELECT create_reference_table('school_type');
            SELECT create_distributed_table('district', 'id');
            """;

        command.ExecuteNonQuery();
    }

    [Fact]
    public void Fails_When_Creating_Reference_Table_When_It_Contains_Inbound_FK()
    {
        using var connection = citus.CreateConnection();
        connection.Open();
        using var tx = connection.BeginTransaction();

        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE district (
                id UUID PRIMARY KEY,
                name TEXT NOT NULL
            );

            CREATE TABLE school_type (
                id uuid NOT NULL,
                name text NOT NULL,
                CONSTRAINT pk_school_type PRIMARY KEY (id)
            );

            CREATE TABLE school (
                id uuid NOT NULL,
                district_id uuid NOT NULL,
                name text NOT NULL,
                school_type_id uuid NOT NULL,
                CONSTRAINT pk_school PRIMARY KEY (id, district_id),
                CONSTRAINT ak_school_district_id_id UNIQUE (district_id, id),
                CONSTRAINT fk_school_districts_district_id FOREIGN KEY (district_id) REFERENCES district (id) ON DELETE CASCADE,
                CONSTRAINT fk_school_school_type_school_type_id FOREIGN KEY (school_type_id) REFERENCES school_type (id) ON DELETE CASCADE
            );
            """;

        command.ExecuteNonQuery();

        // This fails because when the distributed scheme is created, the reference
        // table has an inbound FK that is no longer valid.
        command.CommandText = """
            -- Mark tables as referenced and distributed
            SELECT create_reference_table('school_type');
            SELECT create_distributed_table('district', 'id');
            -- Fails on the prior statement because of the inbound FK
            -- SELECT create_distributed_table('school', 'district_id');
            """;

        // ❌ Reference tables and local tables can only have foreign keys to reference tables and local tables
        Assert.Throws<PostgresException>(() => command.ExecuteNonQuery());
    }

    [Fact]
    public void Fails_When_Creating_Reference_Table_Even_After_Distributed_Table()
    {
        using var connection = citus.CreateConnection();
        connection.Open();
        using var tx = connection.BeginTransaction();

        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE district (
                id UUID PRIMARY KEY,
                name TEXT NOT NULL
            );

            CREATE TABLE school_type (
                id uuid NOT NULL,
                name text NOT NULL,
                CONSTRAINT pk_school_type PRIMARY KEY (id)
            );

            CREATE TABLE school (
                id uuid NOT NULL,
                district_id uuid NOT NULL,
                name text NOT NULL,
                school_type_id uuid NOT NULL,
                CONSTRAINT pk_school PRIMARY KEY (id, district_id),
                CONSTRAINT ak_school_district_id_id UNIQUE (district_id, id),
                CONSTRAINT fk_school_districts_district_id FOREIGN KEY (district_id) REFERENCES district (id) ON DELETE CASCADE,
                CONSTRAINT fk_school_school_type_school_type_id FOREIGN KEY (school_type_id) REFERENCES school_type (id) ON DELETE CASCADE
            );
            """;

        command.ExecuteNonQuery();

        // This also fails because the distributed table cannot have an FK to a non-distributed
        // or non-reference table.
        command.CommandText = """
            -- Mark tables as referenced and distributed
            SELECT create_distributed_table('district', 'id');
            SELECT create_distributed_table('school', 'district_id');
            SELECT create_reference_table('school_type');
            """;

        // ❌ To enforce foreign keys, the referencing and referenced rows need to be stored on the same node
        Assert.Throws<PostgresException>(() => command.ExecuteNonQuery());
    }

    [Fact]
    public void Fails_When_Creating_In_One_Transaction()
    {
        using var connection = citus.CreateConnection();
        connection.Open();
        // ❌ TX causes this to fail
        using var tx = connection.BeginTransaction();

        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE district (
                id UUID PRIMARY KEY,
                name TEXT NOT NULL
            );

            CREATE TABLE school_type (
                id uuid NOT NULL,
                name text NOT NULL,
                CONSTRAINT pk_school_type PRIMARY KEY (id)
            );

            -- Mark tables as referenced and distributed
            SELECT create_reference_table('school_type');
            SELECT create_distributed_table('district', 'id');

            CREATE TABLE school (
                id uuid NOT NULL,
                district_id uuid NOT NULL,
                name text NOT NULL,
                school_type_id uuid NOT NULL,
                CONSTRAINT pk_school PRIMARY KEY (id, district_id),
                CONSTRAINT ak_school_district_id_id UNIQUE (district_id, id),
                CONSTRAINT fk_school_districts_district_id FOREIGN KEY (district_id) REFERENCES district (id) ON DELETE CASCADE
            );

            SELECT create_distributed_table('school', 'district_id');

            -- ❌ Constraint can't be added in same transaction
            ALTER TABLE school
                ADD CONSTRAINT fk_school_school_type_school_type_id
                FOREIGN KEY (school_type_id)
                REFERENCES school_type(id);
            """;

        // ❌ When there is a foreign key to a reference table, Citus needs to perform all operations over a single connection per node to ensure consistency.
        Assert.Throws<PostgresException>(() => command.ExecuteNonQuery());
    }

    [Fact]
    public void Add_FK_To_Reference_Table_Succeeds_When_Two_Separate_Transactions()
    {
        var suffix = Random.Shared.GetHexString(4, true);

        using var firstConnection = citus.CreateConnection();
        firstConnection.Open();
        // ✅ No TX so this will work

        using var command = firstConnection.CreateCommand();
        command.CommandText = $"""
            CREATE TABLE district_{suffix} (
                id UUID PRIMARY KEY,
                name TEXT NOT NULL
            );

            CREATE TABLE school_type_{suffix} (
                id uuid NOT NULL,
                name text NOT NULL,
                CONSTRAINT pk_school_type_{suffix} PRIMARY KEY (id)
            );

            -- Mark tables as referenced and distributed
            SELECT create_reference_table('school_type_{suffix}');
            SELECT create_distributed_table('district_{suffix}', 'id');

            CREATE TABLE school_{suffix} (
                id uuid NOT NULL,
                district_id uuid NOT NULL,
                name text NOT NULL,
                school_type_id uuid NOT NULL,
                CONSTRAINT pk_school_{suffix} PRIMARY KEY (id, district_id),
                CONSTRAINT ak_school_district_id_id_{suffix} UNIQUE (district_id, id),
                CONSTRAINT fk_school_districts_district_id_{suffix} FOREIGN KEY (district_id) REFERENCES district_{suffix} (id) ON DELETE CASCADE
            );

            SELECT create_distributed_table('school_{suffix}', 'district_id');

            ALTER TABLE school_{suffix}
                ADD CONSTRAINT fk_school_school_type_school_type_id_{suffix}
                FOREIGN KEY (school_type_id)
                REFERENCES school_type_{suffix}(id);
            """;

        command.ExecuteNonQuery();
    }

    /// <summary>
    /// We can reuse the connection and create another command.  But cannot use
    /// transaction since we need the distribution to complete and then add the
    /// constraint.
    /// </summary>
    [Fact]
    public void Add_FK_To_Reference_Table_Succeeds_When_One_Connection_Two_Transactions()
    {
        var suffix = Random.Shared.GetHexString(4, true);

        using var firstConnection = citus.CreateConnection();
        firstConnection.Open();

        using var command = firstConnection.CreateCommand();
        command.CommandText = $"""
            CREATE TABLE district_{suffix} (
                id UUID PRIMARY KEY,
                name TEXT NOT NULL
            );

            CREATE TABLE school_type_{suffix} (
                id uuid NOT NULL,
                name text NOT NULL,
                CONSTRAINT pk_school_type_{suffix} PRIMARY KEY (id)
            );

            -- Mark tables as referenced and distributed
            SELECT create_reference_table('school_type_{suffix}');
            SELECT create_distributed_table('district_{suffix}', 'id');

            CREATE TABLE school_{suffix} (
                id uuid NOT NULL,
                district_id uuid NOT NULL,
                name text NOT NULL,
                school_type_id uuid NOT NULL,
                CONSTRAINT pk_school_{suffix} PRIMARY KEY (id, district_id),
                CONSTRAINT ak_school_district_id_id_{suffix} UNIQUE (district_id, id),
                CONSTRAINT fk_school_districts_district_id_{suffix} FOREIGN KEY (district_id) REFERENCES district_{suffix} (id) ON DELETE CASCADE
            );

            SELECT create_distributed_table('school_{suffix}', 'district_id');
            """;

        command.ExecuteNonQuery();

        // Use same connection
        using var secondCommand = firstConnection.CreateCommand();
        secondCommand.CommandText = $"""
            ALTER TABLE school_{suffix}
                ADD CONSTRAINT fk_school_school_type_school_type_id_{suffix}
                FOREIGN KEY (school_type_id)
                REFERENCES school_type_{suffix}(id);
            """;

        secondCommand.ExecuteNonQuery();
    }

    /// <summary>
    /// Adding the reference table AFTER the distribution table can be done with the
    /// FK in the same operation.
    /// </summary>
    [Fact]
    public void Add_Reference_Table_Succeeds_After_Adding_Distribute_Table()
    {
        var suffix = Random.Shared.GetHexString(4, true);

        using var connection = citus.CreateConnection();
        connection.Open();

        // Using a TX causes it to fail
        // using var tx = connection.BeginTransaction();

        using var command = connection.CreateCommand();
        command.CommandText = $"""
            CREATE TABLE district_{suffix} (
                id UUID PRIMARY KEY,
                name TEXT NOT NULL
            );

            CREATE TABLE school_{suffix} (
                id uuid NOT NULL,
                district_id uuid NOT NULL,
                name text NOT NULL,
                school_type_id uuid NOT NULL,
                CONSTRAINT pk_school_{suffix} PRIMARY KEY (id, district_id),
                CONSTRAINT ak_school_district_id_id_{suffix} UNIQUE (district_id, id),
                CONSTRAINT fk_school_districts_district_id_{suffix} FOREIGN KEY (district_id) REFERENCES district_{suffix} (id) ON DELETE CASCADE
            );

            -- Mark tables as referenced and distributed
            SELECT create_distributed_table('district_{suffix}', 'id');
            SELECT create_distributed_table('school_{suffix}', 'district_id');

            CREATE TABLE school_type_{suffix} (
                id uuid NOT NULL,
                name text NOT NULL,
                CONSTRAINT pk_school_type_{suffix} PRIMARY KEY (id)
            );

            -- Mark tables as referenced and distributed
            SELECT create_reference_table('school_type_{suffix}');

            ALTER TABLE school_{suffix}
                ADD CONSTRAINT fk_school_school_type_school_type_id_{suffix}
                FOREIGN KEY (school_type_id)
                REFERENCES school_type_{suffix}(id);
            """;

        command.ExecuteNonQuery();
    }
}
