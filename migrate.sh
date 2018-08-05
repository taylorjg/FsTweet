DIR=$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )

DEFAULT_CONNECTION_STRING="Server=localhost;Port=5432;User Id=postgres;Password=test;Database=FsTweet;"
CONNECTION_STRING=${FSTWEET_DB_CONN_STRING:-${DEFAULT_CONNECTION_STRING}}

dotnet build \
    "${DIR}/src/FsTweet.Db.Migrations/FsTweet.Db.Migrations.fsproj" \
    --output "${DIR}/dist"

mono "packages/database/FluentMigrator.Console/net461/any/Migrate.exe" \
    --assembly "${DIR}/dist/FsTweet.Db.Migrations.dll" \
    --provider "Postgres" \
    --connectionString "${CONNECTION_STRING}"
