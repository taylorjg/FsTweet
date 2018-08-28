FROM microsoft/dotnet:2.1-runtime
WORKDIR /app
COPY src/FsTweet.Web/bin/Release/netcoreapp2.1/publish ./
CMD dotnet FsTweet.Web.dll
