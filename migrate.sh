DIR=$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )

DEFAULT_DATABASE_URL="postgres://postgres:test@localhost:5432/FsTweet"
DATABASE_URL=${DATABASE_URL:-${DEFAULT_DATABASE_URL}}

USERNAME=`echo $DATABASE_URL | cut -f 3 -d '/' | cut -f 1 -d ':'`
PASSWORD=`echo $DATABASE_URL | cut -f 3 -d '/' | cut -f 2 -d ':' | cut -f 1 -d '@'`
HOST=`echo $DATABASE_URL | cut -f 3 -d '/' | cut -f 2 -d ':' | cut -f 2 -d '@'`
PORT=`echo $DATABASE_URL | cut -f 3 -d '/' | cut -f 3 -d ':'`
DATABASE=`echo $DATABASE_URL | cut -f 4 -d '/'`

if [ "$1" == "-ssl" ]; then
  CONNECTION_STRING_SUFFIX="SSL Mode=Require;Trust Server Certificate=true;"
else
  CONNECTION_STRING_SUFFIX=""
fi

CONNECTION_STRING="Server=$HOST;Port=$PORT;User Id=$USERNAME;Password=$PASSWORD;Database=$DATABASE;$CONNECTION_STRING_SUFFIX"

dotnet build "${DIR}/src/FsTweet.Db.Migrations/FsTweet.Db.Migrations.fsproj"

mono "${HOME}/.nuget/packages/fluentmigrator.console/3.1.3/net461/any/Migrate.exe" \
    --assembly "${DIR}/src/FsTweet.Db.Migrations/bin/Debug/netcoreapp2.1/FsTweet.Db.Migrations.dll" \
    --provider "Postgres" \
    --connectionString "${CONNECTION_STRING}"
