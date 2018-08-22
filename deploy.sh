dotnet publish src/FsTweet.Web/FsTweet.Web.fsproj -c Release
heroku container:push web
heroku container:release web
