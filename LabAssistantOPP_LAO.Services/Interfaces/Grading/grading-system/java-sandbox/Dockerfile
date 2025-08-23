# Base image nhẹ có JDK 17
FROM openjdk:17-slim

WORKDIR /app

# Cài đặt các công cụ cần thiết + Docker CLI
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
    unzip \
    time \
    findutils \
    docker.io \
    && rm -rf /var/lib/apt/lists/*

# Tạo thư mục cần thiết
RUN mkdir -p /app/worker_temp /app/submissions

# Copy code worker nếu cần
COPY . .

# Container mặc định chạy ở chế độ idle (để chờ job)
CMD ["tail", "-f", "/dev/null"]
