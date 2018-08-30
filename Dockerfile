FROM microsoft/dotnet:2.1-sdk as build
WORKDIR /app
COPY src/FsTweet.Web .
RUN dotnet publish FsTweet.Web.fsproj -c Release
