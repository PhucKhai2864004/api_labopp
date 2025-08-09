# Sử dụng base image nhẹ chứa JDK 17
FROM openjdk:17-slim

# Đặt thư mục làm việc mặc định bên trong container
WORKDIR /app

# Cài thêm unzip, time, findutils để giải nén bài nộp, đo thời gian, và tìm file .java
RUN apt-get update && \
    apt-get install -y unzip time findutils && \
    rm -rf /var/lib/apt/lists/*

# Lệnh mặc định (nếu không override khi docker run)
CMD ["bash"]
