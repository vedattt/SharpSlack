using SharpSlack;

Console.WriteLine("Starting test");
const string token = "";
const string appToken = "";

var client = new SlackClient(token);

await client.ConnectSocketMode(appToken);
Console.ReadKey();

/*
var responseOutputPostMessage = await client.PostMessage("C04471BCJGN", "test text");
Console.WriteLine(responseOutputPostMessage);

var conversationList = await client.GetConversationList();
foreach (var channel in conversationList)
{
    Console.WriteLine($"ID {channel.Id}: {channel.Name}");
}

var generalChannel = await client.FindChannelByName("general");
Console.WriteLine($"Channel '{generalChannel.Name}' has ID {generalChannel.Id}");

var userEmailLookup = await client.FindUserByEmail("");
Console.WriteLine($"Found user with email {userEmailLookup.Profile.Email}: {userEmailLookup.Id}");

var userList = await client.GetUserList();
foreach (var user in userList)
{
    Console.WriteLine($"ID {user.Id}: {user.Profile.Email}");
}

var openedConversation = await client.OpenDirectMessageConversation(new List<string> { "" });
Console.WriteLine($"Opened conversation with ID {openedConversation.Id}");

var responseOutputPostDirectMessage = await client.PostMessage(openedConversation.Id, "test text");
Console.WriteLine(responseOutputPostDirectMessage);


var fileContents = File.ReadAllBytes("/home/ved/tempfile.txt");
Console.WriteLine(string.Join(",", fileContents.Select(b => b.ToString())));
var responseOutputUploadFile = await client.UploadFile(new List<string> {openedConversation.Id}, fileContents, "test_file.txt", "test file");
Console.WriteLine(responseOutputUploadFile);
*/

