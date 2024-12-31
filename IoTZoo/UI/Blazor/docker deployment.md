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
stop existing container, remove existing container, remove image
**

docker stop $(docker ps -a -q);
docker rm $(docker ps -a -q);
docker rmi $(docker images -a -q);

**
build image and run container
**

docker build --force-rm  -f ./UI/Blazor/Dockerfile -t iotzoo .;
docker save iotzoo -o iotzoo.tar;
docker load -i iotzoo.tar;
docker run -e TZ=Europe/Berlin -p 8085:8080 -p 1883:1883 -d --restart  always iotzoo:latest;


http://raspberrypi:8085/



-------------------------------------------------------------------------------
using docker compose, if you want to use a postgres database instead of Sqlite.