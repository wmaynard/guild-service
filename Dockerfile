FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY publish .
ENTRYPOINT ["dotnet", "guild-service.dll"]
RUN addgroup --system --gid 1000 rumblegroup && adduser --system --uid 1000 --ingroup rumblegroup --shel /bin/sh rumbleuser
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER 1000
