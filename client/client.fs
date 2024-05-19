open System
open System.Net
open System.Net.Sockets
open System.IO

let startClient () =
    try
        let client = new TcpClient()
        client.Connect(IPAddress.Parse("127.0.0.1"), 12345) //Client connected to server
        
        //Initialize reader,writer and stream
        let stream = client.GetStream()
        let reader = new StreamReader(stream)
        let writer = new StreamWriter(stream)

        let welcomeMessage = reader.ReadLine()
        Console.WriteLine(welcomeMessage)

        let rec inputHandler() =
            Console.Write("Sending Command: ")
            let expression = Console.ReadLine()
            writer.WriteLine(expression)
            writer.Flush()
            let response = reader.ReadLine()
            if expression.ToLower() = "bye" then //client sends bye
                if response = "-5" then
                    Console.WriteLine("exit")
                    client.Close() //exit client thread for bye
            elif response = "-5" then //client sends terminate
                client.Close() //exit client thread for terminate
            elif response = "-4" then
                Console.WriteLine("Server response: one or more of the inputs contain(s) non-number(s)")
                inputHandler() // Recursive call to continue handling input
            elif response = "-3" then
                Console.WriteLine("Server response: number of inputs is more than four")
                inputHandler() // Recursive call to continue handling input
            elif response = "-2" then
                Console.WriteLine("Server response: number of inputs is less than two")
                inputHandler() // Recursive call to continue handling input
            elif response = "-1" then
                Console.WriteLine("Server response: incorrect operation command")
                inputHandler() // Recursive call to continue handling input
            else
                Console.WriteLine("Server response: "+response)
                inputHandler() // Recursive call to continue handling input
    // Start handling input
        inputHandler()

    with
        ex -> Console.WriteLine("Client error: " + ex.Message)

startClient()