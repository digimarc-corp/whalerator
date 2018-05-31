export class History {
    public command: String;
    public created: String;
    public type: String;
    public args: String;

    public static From(obj: any): History {
        const history = new History();
        history.command = obj.shortCommand;
        history.created = obj.created;
        history.type = obj.type;
        history.args = (obj.commandArgs) ? obj.commandArgs.trim() : null;
        return history;
    }
}
