// @ts-check
import {
    InworldClient, InworldConnectionService, InworldPacket
} from '@inworld/nodejs-sdk';
import InworldSocketController from './InworldSocketController.js';

const CHARACTER_LIST = [
    "benjamin_steel",
    "brucie",
    "carlos_morales",
    "damien_vex",
    "eddie_thompson",
    "edna_quirke",
    "emily_martinez",
    "frank_thompson",
    "lucas_blackwood",
    "oliver_bellamy",
    "olivia_van_der_woodsen",
    "sergeant_alex_mercer",
    "tony_russo",
    "vincent_the_viper_moretti"
];

const WorkshopName = "gtaworkspace"
const defaultConfigurationConnection = {
    autoReconnect: true,
    disconnectTimeout: 3600 * 60
}

export default class InworldClientManager {
    private connection: InworldConnectionService;
    private client: InworldClient;
    private IsConnected: boolean;
    private isVoiceStarted: boolean;
    private socketController: InworldSocketController;
    private connectedId: string;

    currentCapabilities = {
        audio: true,
        emotions: true,
        phonemes: true
    }

    constructor() {
        this.SetupWorkspaceAndClient();
    }

    async SetupWorkspaceAndClient() {
        this.CreateClient();
    }

    async ConnectToCharacterViaSocket(characterId: string, playerName: string, socket: WebSocket) {
        let matchingIds = CHARACTER_LIST.filter(singleId => singleId.toLowerCase().includes(characterId.toLowerCase()));
        if (matchingIds.length == 0) {
            let errorResult = `Cannot connect to ${characterId}`;
            console.error(errorResult);
            return;
        }
        let id = matchingIds[0];
        console.log("Requesting connecting to " + id);
        let scene = "workspaces/" + WorkshopName + "/characters/{CHARACTER_NAME}".replace("{CHARACTER_NAME}", id);
        this.client.setUser({
            fullName: playerName
        });
        this.client.setScene(scene);

        this.socketController = new InworldSocketController(socket);
        this.client.setOnMessage((dat: any) => this.socketController.ProcessMessage(dat));

        this.client.setOnError((err) => {
            if (err.code != 10 && err.code != 1)
                console.error(err);
        });
        this.connection = this.client.build();
        this.IsConnected = true;
        await this.connection.sendAudioSessionStart();
        this.isVoiceStarted = true;
        console.log("Connection should be established on client side for " + id);
        let verifyConnection = {
            "message": "",
            "type": "established",
        }
        socket.send(JSON.stringify(verifyConnection));
    }

    Say(message: string) {
        if (this.IsConnected) {
            this.connection.sendText(message);
        }
    }

    async TriggerEvent(eventid: string) {
        if (this.IsConnected) {
            console.log("Sending trigger to Inworld Servers. Id: " + eventid);
            await this.connection.sendTrigger(eventid);
        }
    }

    async StartVoice() {
        if (this.connection == null) return;
        if (!this.isVoiceStarted) {
            console.log("Starting audio session with the character...");
            await this.connection.sendAudioSessionStart();
            this.isVoiceStarted = true;
        }
    }

    async SendVoice(voiceChunk: string) {
        if (this.connection == null) return;
        let isActive = this.connection.isActive();
        if (isActive) {
            await this.connection.sendAudio(voiceChunk);
            console.log("Player is talking to character...");
        } else {
            console.log("Restarting audio session, seems like it was inactive");
            await this.connection.sendAudioSessionStart();
            await this.connection.sendAudio(voiceChunk);
            console.log("Player is talking to character...");
            isActive = true;
            this.isVoiceStarted = true;
        }
    }

    async PauseVoice() {
        if (this.isVoiceStarted) {
            console.log("Stopping audio session with the character...");
            await this.connection.sendAudioSessionEnd();
            this.isVoiceStarted = false;
        }
    }

    Disconnect() {
        console.log("Disconnecting with the character...");
        this.IsConnected = false;
        this.connection.close();
        this.connection = null;
    }

    Reconnect() {
        if (this.IsConnected) {
            console.log("Reconnecting to character...");
            this.connection.sendText("hmm");
        }
    }

    CreateClient() {
        this.client = new InworldClient();
        this.client.setApiKey({
            key: process.env.INWORLD_KEY as string,
            secret: process.env.INWORLD_SECRET as string,
        });
        this.client.setConfiguration({
            connection: defaultConfigurationConnection,
            capabilities: this.currentCapabilities
        });
    }
}