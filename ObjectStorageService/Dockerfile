FROM minio/mc:latest
COPY ../docker/init/30-init-minio.sh .
RUN chmod +x ../docker/init/30-init-minio.sh
ENTRYPOINT ["../docker/init/30-init-minio.sh"]
