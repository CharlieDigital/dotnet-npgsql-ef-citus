# Citus Workbench

A workbench for Citus.

This repository includes:

- Test fixtures for starting Citus using [Testcontainers](https://dotnet.testcontainers.org/)
- Integration tests working with Citus using raw SQL statements
- INtegration tests working with Citus using EF and proper modeling to support migrations + distribution

## Run The Tests

```shell
cd tests

# EF based tests
dotnet test --filter CitusEfTests

# Direct SQL tests
dotnet test --filter CitusDirectSqlTests

```

## Resources

- [Single Node Citus](https://www.citusdata.com/blog/2021/03/20/sharding-postgres-on-a-single-citus-node/)
- [Citus in Cluster](https://marc.helbling.fr/running-citus-locally/)
- [Citus in Cluster (Compose)](https://raw.githubusercontent.com/citusdata/docker/master/docker-compose.yml_)
