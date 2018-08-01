dotnet publish src/FsTweet.fsproj -c Release
heroku container:push web
heroku container:release web
