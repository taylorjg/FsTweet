# Building and running

I decided not to use `forge`. I am using `paket` and `fake` directly.

```
./fake.sh build
./fake.sh build -t Run
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
