On rasperry pi install Docker

curl -fsSL https://get.Docker.com -o get-Docker.sh

sudo sh get-Docker.sh


copy the source code to the Raspberry Pi. A convinient method is to do this with WinSCP.


sudo su

switch to the folder where the .sln file is located (folder Server)

It is best to do this directly on the Raspberry Pi, then it is automatically built for the correct 
processor architecture.

The disadvantage is that this takes a little longer, about 10 minutes on a pi 3 but only 2 minutes on a pi 5.

**
stop existing containers and remove them (the volume with the databases is kept)
**

docker compose down --rmi local

**
build image and start containers
**

docker compose up -d --build

docker compose ps


http://raspberrypi:8085/

Note: The SQLite databases are stored in the named Docker volume "iotzoo_databases" (mounted at /app/Database/Data), not in the container or the copied source folder. Copying a newer version to the Raspberry Pi and rebuilding with "docker compose up -d --build" keeps the databases. They are only deleted by "docker compose down -v" or "docker volume rm". Back up the volume before removing it.


-------------------------------------------------------------------------------
the compose file also contains a postgres service (currently commented out). Uncomment it in docker-compose.yml if you want to use a postgres database instead of Sqlite.