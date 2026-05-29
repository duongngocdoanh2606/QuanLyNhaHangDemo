# Bước 1: Sử dụng SDK để build ứng dụng
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /app

# Copy file solution và file dự án vào để khôi phục các thư viện
COPY *.sln ./
COPY QuanLyNhaHangDemo/*.csproj ./QuanLyNhaHangDemo/
RUN dotnet restore

# Copy toàn bộ code còn lại vào và thực hiện build xuất bản (publish)
COPY . ./
RUN dotnet publish -c Release -o out

# Bước 2: Sử dụng Runtime để chạy ứng dụng
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build-env /app/out .

# Cấu hình Port bắt buộc cho Railway
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Chạy ứng dụng (file .dll chính)
ENTRYPOINT ["dotnet", "QuanLyNhaHangDemo.dll"]