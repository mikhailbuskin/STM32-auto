const http = require('http');
const fs = require('fs');
const path = require('path');
const express = require('express');

const WebSocket = require('ws');
const { findPort, listPorts } = require('./port.js');
const { SerialPort } = require('serialport')


async function start() {

    // get arguments from command line, to take port path or "list" command to list available ports
    var portMeta = await findPort('wch.cn');
    // if not found, list available ports
    if (portMeta == null) {
        listPorts();
    }
    var portName = portMeta ? portMeta.path : null;
    
    // create web socket
    // take HTTP port from environment variable or use default
    const HTTP_PORT = process.env.HTTP_PORT || 6060;

    // Serve static files (e.g., index.html) from the "public" directory
    const app = express();
    app.use(express.static(path.join(__dirname, 'public')));
    const server = http.createServer(app);
    
    // create websocket when we connected to serial port
    if (portName) {
        // create serial port
        const port = new SerialPort({ path: portName, baudRate: 9600 });
        const wss = new WebSocket.Server({ server }); // Bind WebSocket to the HTTP server

        wss.on('connection', (ws) => {
            console.log('WebSocket client connected');

            const onData = data => {
                ws.send(data);
            };

            port.on('data', onData);

            ws.on('close', () => {
                console.log('WebSocket client disconnected');
                port.off('data', onData);
            });

            ws.on('error', (err) => {
                console.error('WebSocket error:', err);
                port.off('data', onData);
            });

            ws.on('message', (data) => {
                console.log('WebSocket received:', new Date().toISOString());
                // data
                console.log(data);
                port.write(data);
            });
        });

        // receive data from websocket and send to serial port
        // wss.on('message', (data) => {
        //     console.log('WebSocket sent:', new Date().toISOString());
        //     port.write(data);
        // });

        port.on('close', () => {
            console.log('Serial port closed');
            wss.clients.forEach((client) => {
                client.close();
            });
        });

        port.on('error', (err) => {
            console.error('Serial port error:', err);
            wss.clients.forEach((client) => {
                client.close();
            });
        });
    }
    // Start the HTTP server
    server.listen(HTTP_PORT, () => {
        console.log(`HTTP Server running on http://localhost:${HTTP_PORT}`);
        if (portName) {
            console.log(`Listening on serial port: ${portName}`);
        }
    });

    
};
start();
