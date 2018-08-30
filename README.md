# Differences from the book

## Not using Forge, Fake or Paket

I decided not to use any of the following:

* [Forge](https://github.com/fsharp-editing/Forge)
* [FAKE](https://github.com/fsharp/FAKE)
* [Paket](https://fsprojects.github.io/Paket/)

Actually, I did initially use `FAKE` and `Paket` but got rid of them after getting all the code working.

I added the following to `src/FsTweet.Web/FsTweet.Web.fsproj` to copy the `assets` and `views` directories:

```
  <Target Name="CopyCustomContentAfterBuild" AfterTargets="AfterBuild">
    <Exec Command="cp -r assets views $(OutDir)" />
  </Target>
  <Target Name="CopyCustomContentAfterPublish" AfterTargets="Publish">
    <Exec Command="cp -r assets views $(PublishDir)" />
  </Target>
```

## Not using SQLProvider

I am using
[Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
instead of
[SQLProvider](https://github.com/fsprojects/SQLProvider)
because I could not get `SQLProvider` to work on .NET Core on macOS.

## Not deploying to Azure

I decided to deploy to
[Heroku](https://www.heroku.com/)
instead of
[Azure](https://azure.microsoft.com/)
because I have more experience with `Heroku`. I am
[deploying with Docker](https://devcenter.heroku.com/categories/deploying-with-docker).

# Building

```
dotnet clean src/FsTweet.Web/FsTweet.Web.fsproj
dotnet build src/FsTweet.Web/FsTweet.Web.fsproj
```

# Running locally

```
docker run --name postgres -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=test -p5432:5432 -d postgres:10.4-alpine
export DATABASE_URL=postgres://postgres:test@localhost:5432/FsTweet
export FSTWEET_ENVIRONMENT=dev
export FSTWEET_SUAVE_SERVER_KEY=<Suave server key>
export FSTWEET_SENDER_EMAIL_ADDRESS=<email address>
export FSTWEET_POSTMARK_SERVER_KEY=<Postmark server key>
export FSTWEET_POSTMARK_TEMPLATE_ID=<Postmark template id>
export FSTWEET_STREAM_API_KEY=<Stream api key>
export FSTWEET_STREAM_API_SECRET=<Stream api secret>
export FSTWEET_STREAM_APPID=<Stream app id>
dotnet run --project src/FsTweet.Web/FsTweet.Web.fsproj
```

I use `DATABASE_URL` instead of `FSTWEET_DB_CONN_STRING` because I deploy to Heroku
and use the
[Heroku Postgres add-on](https://www.heroku.com/postgres).
As part of provisioning this add-on, Heroku adds the `DATABASE_URL` config var which has
the following format:

```
postgres://{user}:{password}@{hostname}:{port}/{database-name}
```

# Problems

## DotLiquid `extends`

I encountered a problem with `extends`:

```
{% extends master_page.liquid %}
```

What was rendered in the browser was just the following error message:

```
Liquid error: Value cannot be null. Parameter name: path2
```

After some digging/experimentation, I got it working by doing this:

```
{% extends 'master_page.liquid' %}
```

# Links

* [F# Applied II](https://www.demystifyfp.com/FsApplied2/)
