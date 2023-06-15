FROM mariadb as runtime
WORKDIR .
EXPOSE 3308

COPY ./00_init.sql /docker-entrypoint-initdb.d/00_init.sql


ENV MYSQL_ROOT_PASSWORD="123456"
ENV MYSQL_TCP_PORT=3308