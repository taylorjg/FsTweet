# Differences from the book

## Not using Forge, Fake or Paket

I decided not to use any of the following:

* [Forge](https://github.com/fsharp-editing/Forge)
* [FAKE](https://github.com/fsharp/FAKE)
* [Paket](https://fsprojects.github.io/Paket/)

Actually, I did initially use `FAKE` and `Paket` but got rid of them after getting all the code working.

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
