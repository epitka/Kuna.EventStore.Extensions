    
    // filtering events
    "Filter": {
      "Type": "EventType",
      "Expression": "regex:(^ShoppingCartOpened|^ShoppingCartCanceled)"
    },

    // filtering streams
    "Filter": {
      "Type": "StreamName",
      "Expression": "prefix:ShoppingCart"
    },