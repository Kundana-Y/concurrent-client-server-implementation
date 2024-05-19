open System
open System.Net
open System.Net.Sockets
open System.Text
open System.IO
open System.Threading.Tasks
open System.Collections.Concurrent

let mutable serverSocket : TcpListener = null
let mutable clientCounter = 0
let connectedClients = ConcurrentDictionary<int, TcpClient>()
let mutable continueAcceptingClients = true

let startServer () =
    try
        serverSocket <- new TcpListener(IPAddress.Parse("127.0.0.1"), 12345)
        serverSocket.Start()
        Console.WriteLine("Server started. Listening on port 12345.")

        let handleClientAsync (clientSocket: TcpClient) =
            async {
                let currentClient = clientCounter + 1
                clientCounter <- currentClient // Establishing client connection

                use stream = clientSocket.GetStream()
                use reader = new StreamReader(stream)
                use writer = new StreamWriter(stream)
                let mutable continueHandling = true
                
                writer.WriteLine("Hello!") //Responding to client after successful connection
                writer.Flush()

                while continueHandling do
                    let! data = reader.ReadLineAsync() |> Async.AwaitTask

                    if String.IsNullOrEmpty(data) then
                        continueHandling <- false
                    else
                        Console.WriteLine($"Received: " + data)

                    let parts = data.Split(' ')
                    
                    //received terminate
                    if parts.[0] = "terminate" && parts.Length = 1 then 
                        writer.WriteLine("-5")
                        let allClients = connectedClients.Values
                        let mutable i = 0
                        Console.WriteLine($"Responding to client {currentClient} with result: -5")
                        for c in allClients do
                            if c.Connected then
                                i <- i + 1
                                use clientStream = c.GetStream()
                                use clientWriter = new StreamWriter(clientStream)
                                clientWriter.WriteLine("-5")
                                clientWriter.Flush()
                                c.Close() //closing all client connections
                                Console.WriteLine($"client {i-1} connection status: {c.Connected}") 
                        connectedClients.Clear()
                        
                        continueHandling <- false
                        continueAcceptingClients <- false
                        Environment.Exit(0)

                    //received bye
                    elif parts.[0] = "bye" && parts.Length = 1 then 
                        writer.WriteLine("-5")
                        Console.WriteLine($"Responding to client {currentClient} with result: -5")
                        let _ = connectedClients.TryRemove(currentClient) //remove that client from active clients
                        continueHandling <- false

                    //check if command length is less than 3
                    elif parts.Length < 3 then
                        //check if it is a invalid command
                        if parts.[0] <> "add" && parts.[0] <> "subtract" && parts.[0] <> "multiply" && parts.[0] <> "bye" && parts.[0] <> "terminate" then
                            Console.WriteLine($"Responding to client {currentClient} with result: -1")
                            writer.WriteLine("-1") //invalid operation command
                        //case where number of inputs are less than 2
                        else 
                            Console.WriteLine($"Responding to client {currentClient} with result: -2")
                            writer.WriteLine("-2") //number of inputs is less than two
                    else
                        let operatorSymbol = parts.[0]
                        let numbers = parts.[1..]
                        let mutable parsedNumbers =
                            try
                               Array.map Int64.Parse numbers
                            with
                               | :? System.FormatException -> [|-4|] //one or more of the inputs contain(s) non-number(s)
                        match operatorSymbol with
                        | "add" ->
                            if parsedNumbers.Length > 4 then
                                writer.WriteLine("-3") //number of inputs is more than four
                                Console.WriteLine($"Responding to client {currentClient} with result: -3")
                            else
                                //result of addition operation sent back to respective client
                                writer.WriteLine(Array.sum parsedNumbers)
                                Console.WriteLine($"Responding to client {currentClient} with result: {Array.sum parsedNumbers}")
                        | "subtract" ->
                            if parsedNumbers.Length > 4 then
                                writer.WriteLine("-3") //number of inputs is more than four
                                Console.WriteLine($"Responding to client {currentClient} with result: -3")
                            else
                                //result of suctraction operation sent back to respective client
                                writer.WriteLine(parsedNumbers.[0] - Array.sum (parsedNumbers.[1..]))
                                Console.WriteLine($"Responding to client {currentClient} with result: {parsedNumbers.[0] - Array.sum (parsedNumbers.[1..])}")
                        | "multiply" ->
                            if parsedNumbers.Length > 4 then
                                writer.WriteLine("-3") //number of inputs is more than four
                                Console.WriteLine($"Responding to client {currentClient} with result: -3")
                            else
                                //result of multiplication operation sent back to respective client
                                writer.WriteLine(Array.fold (fun acc x -> acc * x) parsedNumbers.[0] parsedNumbers.[1..])
                                Console.WriteLine($"Responding to client {currentClient} with result: {Array.fold (fun acc x -> acc * x) parsedNumbers.[0] parsedNumbers.[1..]}")
                        | _ -> 
                            writer.WriteLine("-1") //invalid operation command
                            Console.WriteLine($"Responding to client {currentClient} with result: -1")

                    if clientSocket.Connected then
                        writer.Flush()
                    

            }

        //recursive function to accept incoming clients - handling multiple clients
        let rec acceptClients() =
            async {
                while continueAcceptingClients do
                    let! clientSocket = serverSocket.AcceptTcpClientAsync() |> Async.AwaitTask
                    connectedClients.AddOrUpdate(clientCounter + 1, clientSocket, fun _ old -> old)
                    Task.Run(fun () -> handleClientAsync clientSocket |> Async.RunSynchronously) |> ignore
                }

        let acceptingClients = acceptClients()
        Async.RunSynchronously acceptingClients

    with
    | ex -> Console.WriteLine(ex.Message)

startServer()