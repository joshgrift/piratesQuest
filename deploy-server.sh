# Localhost
scp ./dist/0.5.0/piratesquest-server-linux-x64.zip piratesquest:~/server.zip

# on host
unzip -o server.zip 

# Run
nohup ~/piratesquest-server-linux-x64/piratesquest-server --server-id 2 --server-api-key REDACTED &

# kill existing
pkill -f 'piratesquest-se'

