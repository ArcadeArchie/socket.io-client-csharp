import * as socket from "socket.io"
import * as http from "http";

console.log('socket.io-server');

const server = http.createServer();
const io = socket(server, {
    pingInterval: 10000,
    pingTimeout: 5000,
    transports: ["websocket"]
});

io.use((socket, next) => {
    if (socket.handshake.query.token === "io") {
        next();
    } else {
        next(new Error("Authentication error"));
    }
})

io.on("connection", socket => {
    console.log(`connect: ${socket.id}`);

    socket.on("hi", name => {
        socket.emit("hi", `hi ${name}, You are connected to the server`);
    });

    socket.on("ack", (name, fn) => {
        fn({
            result: true,
            message: `ack(${name})`
        });
    });

    socket.on("bytes", (name, data) => {
        const bytes = Buffer.from(data.bytes.toString() + " - server - " + name, "utf-8");
        socket.emit("bytes", {
            clientSource: data.source,
            source: "server",
            bytes
        });
    });

    socket.on("sever disconnect", close => {
        socket.disconnect(close)
    });

    socket.on("binary ack", (name, data, fn) => {
        const bytes = Buffer.from(data.bytes.toString() + " - server - " + name, "utf-8");
        fn({
            clientSource: data.source,
            source: "server",
            bytes
        });
    });

    socket.on("change", (val1, val2) => {
        socket.emit("change", val2, val1);
    })
});

const nsp = io.of("/nsp");
nsp.on("connection", socket => {
    console.log(`connect: ${socket.id}`);

    socket.on("disconnect", reason => {
        console.log(`disconnect: ${reason}`);
        if (reason === 'io server disconnect') {
            // the disconnection was initiated by the server, you need to reconnect manually
        }
    });

    socket.on("hi", name => {
        socket.emit("hi", `hi ${name}, You are connected to the server - nsp`);
    });

    socket.on("ack", (name, fn) => {
        fn({
            result: true,
            message: `ack(${name})`
        });
    });

    socket.on("bytes", (name, data) => {
        const bytes = Buffer.from(data.bytes.toString() + " - server - " + name, "utf-8");
        socket.emit("bytes", {
            clientSource: data.source,
            source: "server",
            bytes
        });
    });

    socket.on("sever disconnect", close => {
        socket.disconnect(close)
    });

    socket.on("binary ack", (name, data, fn) => {
        const bytes = Buffer.from(data.bytes.toString() + " - server - " + name, "utf-8");
        fn({
            clientSource: data.source,
            source: "server",
            bytes
        });
    });

    socket.on("change", (val1, val2) => {
        socket.emit("change", val2, val1);
    })
});

server.listen(11000);