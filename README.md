# Nudges System POC Repository

Welcome to the Nudges system POC repository.  This repo is meant to contain all of the necessary components of the Nudges system.


# Steps

After a fresh clone, navigate to the root of the repo and...

1. Run `dotnet dev-certs https -ep ./certs/aspnetapp.pfx` ([See here](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-dev-certs) for details)
2. Run `./certs/generate-certs`
3. Run `./start-dev`
4. Wait like 20 minutes for everything to build/start
5. Navigate to `https://localhost:5050` in a browser
6. Log in with the username `+15555555555` and password `pass`

If you see a "View Plans" button, everything is working.


# Creating Your First Plan

TODO
