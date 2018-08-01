FROM microsoft/dotnet:2.1-runtime
WORKDIR /app
COPY src/bin/Release/netcoreapp2.1/publish ./
COPY src/views ./views/
COPY src/assets ./assets/
CMD dotnet FsTweet.dll
