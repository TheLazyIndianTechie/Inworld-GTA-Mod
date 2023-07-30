import * as dotenv from 'dotenv'
import * as fs from 'fs';
import websocketPlugin, { SocketStream } from "@fastify/websocket"
import Fastify, { FastifyRequest } from 'fastify'
import path from "path";
const resolved = path.resolve(".env"); 
console.log("Reading .env from location: ",  resolved);
console.log("Don't worry, you are suppose to see this! This window will list all the actions going on in background. That's important. Don't close this window. Go back to game.");
const fastify = Fastify({
  logger: true
});
fastify.register(websocketPlugin);

import InworldClientManager from "./Inworld/InworldManager.js";
dotenv.config({ path: resolved });
const ClientManager = new InworldClientManager();

OverrideConsole();

process.on('uncaughtException', function (err) {
  console.error('Caught exception: ', err);
  logError(JSON.stringify(err));
});

// This is mostly unused but might needed to check status of "console app"
fastify.get('/ping', (request, reply) => {
  return { 'status': "OK" }
});

// Socket connection for better communication channel
fastify.register(async function (fastify) {
  fastify.get('/chat', { websocket: true }, (connection: SocketStream, req: FastifyRequest) => {
    connection.socket.on('message', msg => {
      try{
        let message = JSON.parse(msg.toString());
        if (message.type == "connect") {
          ClientManager.ConnectToCharacterViaSocket(message.id, message.message, connection.socket);
        } else if (message.type == "message") {
          ClientManager.Say(message.message);
        } else if (message.type == "event") {
          console.log("Event received. Id:", message.id);
          ClientManager.TriggerEvent(message.id);
        }else if (message.type == "user_voice"){
          ClientManager.SendVoice(message.chunk);
        } else if (message.type == "user_voice_pause"){
          ClientManager.PauseVoice();
        } else if (message.type == "reconnect"){
          ClientManager.Reconnect();
        } else if (message.type == "disconnect"){
          ClientManager.Disconnect();
        } else if (message.type == "user_voice_start"){
          ClientManager.StartVoice();
        }
      } catch {
        ClientManager.SendVoice(msg);
      }
    })
  })
});

// Run the server!
const StartEngine = async () => {
  try {
    await fastify.listen({
      port: 3000
    })
  } catch (err) {
    fastify.log.error(err);
    console.error(err);
    logError(JSON.stringify(err));
    process.exit(1)
  }
}

StartEngine();

function OverrideConsole(){
  const originalLog = console.log;
  console.log = function() {
    const timestamp: string = new Date().toISOString();
    const args = Array.prototype.slice.call(arguments);
    args.unshift(`[${timestamp}]`);
    originalLog.apply(console, args);
  };
  const originalError = console.error;
  console.error = function() {
    const timestamp: string = new Date().toISOString();
    const args = Array.prototype.slice.call(arguments);
    args.unshift(`[${timestamp}]`);
    originalError.apply(console, args);
  };
}

function logError(message: string): void {
  const timestamp: string = new Date().toISOString();
  const logMessage: string = `${timestamp} - ${message}`;
  const logFileName = "inworldClient.log"
  if (fs.existsSync(logFileName)) {
    // File exists, append to it
    fs.appendFileSync(logFileName, logMessage + '\n', 'utf8');
  } else {
    // File does not exist, create it and write the log message
    fs.writeFileSync(logFileName, logMessage + '\n', 'utf8');
  }
}