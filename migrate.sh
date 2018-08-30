DIR=$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )

DEFAULT_CONNECTION_STRING="Server=localhost;Port=5432;User Id=postgres;Password=test;Database=FsTweet;"
CONNECTION_STRING=${CONNECTION_STRING:-${DEFAULT_CONNECTION_STRING}}

dotnet build "${DIR}/src/FsTweet.Db.Migrations/FsTweet.Db.Migrations.fsproj"

mono "${HOME}/.nuget/packages/fluentmigrator.console/3.1.3/net461/any/Migrate.exe" \
    --assembly "${DIR}/src/FsTweet.Db.Migrations/bin/Debug/netcoreapp2.1/FsTweet.Db.Migrations.dll" \
    --provider "Postgres" \
    --connectionString "${CONNECTION_STRING}"
