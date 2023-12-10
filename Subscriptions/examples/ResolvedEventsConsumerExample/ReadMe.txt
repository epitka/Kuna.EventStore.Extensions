If you have running container of EventStore then just change connection string in appsetting.json file
If you don't have running container of EventStore then run docker-compose up -d in EventStore folder
You will need to add events to EventStore. To generate some events you can use EventStore UI. To open it go to http://localhost:2113/ and login with admin:changeit credentials. Then go to Streams tab and create stream with name "test-stream". After that you can add events to this stream. You can use this json as example:
```
{
  "eventId": "1",
  "eventType": "TestEvent",
  "data": {
    "test": "test"
  },
  "metadata": {
    "test": "test"
  }
}
```

Also, you can use SeederExample project to generate some events. See SeederExample/Program.cs file for details.