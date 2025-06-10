# Local Postgres Database Creation

This folder contains the scripts and configuration necessary to create an initialize a local Postgres database for local development.  The actual creation is performed by running the `run-backend.sh` script at the root of the repository, but the details of how it works is laid out here.

## Docker

The Postgres database is created as a Docker container.  The `Dockerfile` for it copies a few scripts from the repository to create and populate the relevant tables.

- `userdb.sql` defines the tables for the User domain.
- `init-db.sh` is the script that executes the above.

## Missing Requirements

As noted in the `docker-compse.backend.yml` file run by `run-backend.sh`, there is a file required that is left out of source control in order to set up the database.  For a first-time clone, you must create a `.env.docker` file within this directory.  The template is as follows.

```ini
POSTGRES_PASSWORD=nudges
POSTGRES_USER=postgres

POSTGRES_MULTIPLE_DATABASES=userdb
```

> **NOTE:** The above can actually be used verbatim.

## Modification

In order to update the tables created, simply update the appropriate script from above.

In order to add a new script, you must create the script and add entries in both the `Dockerfile` as well as `init-db.sh`, following the existing conventions.



