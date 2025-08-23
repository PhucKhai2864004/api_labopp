# Sử dụng base image nhẹ chứa JDK 17
FROM openjdk:17-slim

# Đặt thư mục làm việc mặc định bên trong container
WORKDIR /app

# Cài thêm unzip, time, findutils và Docker CLI
RUN apt-get update && \
    apt-get install -y unzip time findutils docker.io && \
    rm -rf /var/lib/apt/lists/*

# Tạo thư mục worker_temp bên trong container
RUN mkdir -p /app/worker_temp

# Lệnh mặc định
CMD ["bash"]
