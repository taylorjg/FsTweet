DIR=$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )

dotnet build \
    "${DIR}/src/FsTweet.Db.Migrations/FsTweet.Db.Migrations.fsproj" \
    --output "${DIR}/dist"

mono "packages/database/FluentMigrator.Console/net461/any/Migrate.exe" \
    --assembly "${DIR}/dist/FsTweet.Db.Migrations.dll" \
    --provider "Postgres" \
    --connectionString "Server=127.0.0.1;Port=5432;Database=fstweet;User Id=postgres;Password=test;"
