export default class InworldSocketController {
    private CombinedResponse: string = "";
    private CombinedUserInput: string = "";

    constructor(private socket: WebSocket) { }

    ProcessMessage(msg: any) {
        if (msg.type == 'AUDIO') {
            let result = {
                "data": msg.audio.chunk,
                "emotion": "none",
                "type": "agent_audio"
            };
            console.log("Character is talking to player..");
            this.socket.send(JSON.stringify(result));
        } else if (msg.emotions) {
            // dont use for now
        } else if (msg.phonemes) {
            // dont use in GTA
        } else if (msg.isText()) {
            if (msg.routing.target.isCharacter) {
                // Always overwrite user input
                this.CombinedUserInput = msg.text.text;
            } else {
                let responseMessage = msg.text.text;
                this.CombinedResponse += responseMessage;
            }
        } else if(msg.isTrigger()){
            let result = {
                "data": JSON.stringify(msg.trigger),
                "event_id": msg.trigger.name,
                "type": "event"
            };
            console.log("Sending trigger to game. Trigger id is: ", msg.trigger.name);
            this.socket.send(JSON.stringify(result));
        }  else if (msg.isInteractionEnd()) {
            let result = {
                "message": this.CombinedResponse,
                "emotion": "none",
                "type": "chat"
            };
            this.CombinedResponse = "";
            this.socket.send(JSON.stringify(result));
        }
    }

    SendUserVoiceInput() {
        let userData = {
            "message": this.CombinedUserInput,
            "emotion": "none",
            "type": "user_transcribe"
        }
        this.socket.send(JSON.stringify(userData));
    }
}