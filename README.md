[![CircleCI](https://circleci.com/gh/taylorjg/FsTweet/tree/master.svg?style=svg)](https://circleci.com/gh/taylorjg/FsTweet/tree/master)

# Differences from the book

## Not using Forge, FAKE or Paket

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
export FSTWEET_SITE_BASE_URL=https://fstweet-jt.herokuapp.com
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

# Deploying

```
./deploy.sh
```

# Migrating the database

## Locally

```
export DATABASE_URL=postgres://postgres:test@localhost:5432/FsTweet
./migrate.sh
```

## On Heroku

```
export DATABASE_URL=postgres://{user}:{password}@{hostname}:{port}/{database-name}
./migrate.sh -ssl
```

The `-ssl` flag adds the following extra connection string parameters:

```
SSL Mode=Require;Trust Server Certificate=true;
```

# TODO

* ~~CI/CD via [CircleCI 2.0](https://circleci.com/)~~
* ~~Add an additional variable to the Postmark email template for the base part of the URL~~
* ~~Add version information and surface it in the Web UI somewhere (e.g. in the page title)~~
* Do some of the suggested excercises
    * Chapter 11
        * Send a welcome email on successful email verification
    * Chapter 15
        * Store user informarion in a database instead of a cookie
        * Use sliding expiration for the session cookie
    * Chapter 19
        * Send an email notification to followee when followee is followed by follower
        * Add support for unfollowing
* Add unit tests
* Add integration tests

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

## Package woes

When I got rid of `Paket`, I re-added all the relevant packages via `dotnet add` e.g.

```
dotnet add src/FsTweet.Web/FsTweet.Web.fsproj package Suave
```

The project built and launched successfully. However, when sending a tweet, I encountered the following exception:

```
System.MissingMethodException: Method not found: 'System.Tuple`2<Microsoft.FSharp.Core.FSharpFunc`2<System.__Canon,Microsoft.FSharp.Core.FSharpOption`1<!!1>>,Microsoft.FSharp.Core.FSharpFunc`2<!!1,Microsoft.FSharp.Core.FSharpFunc`2<System.__Canon,System.__Canon>>> Prism.ofEpimorphism(Microsoft.FSharp.Core.FSharpFunc`2<System.__Canon,Microsoft.FSharp.Core.FSharpOption`1<!!1>>, Microsoft.FSharp.Core.FSharpFunc`2<!!1,System.__Canon>)'.
   at Chiron.Mapping.Json.writeWith[a](FSharpFunc`2 toJson, String key, a value)
   at Social.Suave.ToJson@158-6.Invoke(Unit unitVar) in /Users/jontaylor/HomeProjects/fsharp/tmp/FsTweet/src/FsTweet.Web/Social.fs:line 158
   at Chiron.Builder.Delay@193.Invoke(Json json)
   at Social.Suave.onFindFolloweesSuccess(FSharpList`1 users) in /Users/jontaylor/HomeProjects/fsharp/tmp/FsTweet/src/FsTweet.Web/Social.fs:line 207
   at Chessie.ErrorHandling.AsyncExtensions.Async.map@241.Invoke(a x)
   at Microsoft.FSharp.Control.AsyncBuilderImpl.args@506-1.Invoke(a a)
```

I ran `dotnet publish` for both the `Paket` based and non `Paket` based versions of the code and then compared the `md5` hashes of all the dlls. Most of them were identical. The first one that differed was `Aether.dll` (brought in by `Chiron` ?). A bit of digging revealed that the `Paket` based version was using `8.2` of this dll whereas the non `Paket` based version was using `8.0.2`. I explicitly added version `8.2`:

```
dotnet add src/FsTweet.Web/FsTweet.Web.fsproj package Aether -v 8.2
```

After doing this and rebuilding, the problem went away.

# Links

* [F# Applied II](https://www.demystifyfp.com/FsApplied2/)
