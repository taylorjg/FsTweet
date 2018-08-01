FROM microsoft/dotnet:2.1-runtime
WORKDIR /app
COPY src/FsTweet.Web/bin/Release/netcoreapp2.1/publish ./
COPY src/FsTweet.Web/views ./views/
COPY src/FsTweet.Web/assets ./assets/
CMD dotnet FsTweet.Web.dll
