#pragma once
#include "Defines.hpp"


#ifdef USE_INTERNAL_MQTT
#include "DebugHelper.hpp"
#include <WiFi.h>

#define dbg_ptr uint32_t

#include "StringIndexer.h"
#include "pocos/TopicLink.hpp"

#include <TinyStreaming.h>
#include <set>
#include <string>
#include <vector>


using TcpClient = WiFiClient;
using TcpServer = WiFiServer;

enum InternalMqttError : uint8_t
{
    MqttOk             = 0,
    MqttNowhereToSend  = 1,
    MqttInvalidMessage = 2,
};

using string = TinyConsole::string;

class InternalTopic : public IndexedString
{
  public:
    InternalTopic(const string& m) : IndexedString(m)
    {
    }

    InternalTopic(const char* s, uint8_t len) : IndexedString(s, len)
    {
    }
    
    InternalTopic(const char* s) : InternalTopic(s, strlen(s))
    {
    }

    const char* c_str() const
    {
        return str().c_str();
    }

    bool matches(const InternalTopic&) const;
};

class InternalMqttClient;
class MqttMessage
{
    const uint16_t MaxBufferLength = 4096; // hard limit: 16k due to size decoding
  public:
    enum Type : uint8_t
    {
        Unknown     = 0,
        Connect     = 0x10,
        ConnAck     = 0x20,
        Publish     = 0x30,
        PubAck      = 0x40,
        Subscribe   = 0x80,
        SubAck      = 0x90,
        UnSubscribe = 0xA0,
        UnSuback    = 0xB0,
        PingReq     = 0xC0,
        PingResp    = 0xD0,
        Disconnect  = 0xE0
    };

    enum State : uint8_t
    {
        FixedHeader    = 0,
        Length         = 1,
        VariableHeader = 2,
        PayLoad        = 3,
        Complete       = 4,
        Error          = 5,
        Create         = 6
    };

    static inline uint32_t getSize(const char* buffer)
    {
        const unsigned char* bun = (const unsigned char*)buffer;
        return (*bun << 8) | bun[1];
    }

    MqttMessage()
    {
        reset();
    }

    MqttMessage(Type t, uint8_t bits_d3_d0 = 0)
    {
        create(t);
        buffer[0] |= bits_d3_d0;
    }

    void incoming(char byte);

    void add(char byte)
    {
        incoming(byte);
    }

    void add(const char* p, size_t len, bool addLength = true);

    void add(const string& s)
    {
        add(s.c_str(), s.length());
    }

    void add(const InternalTopic& t)
    {
        add(t.str());
    }

    const char* end() const
    {
        return &buffer[0] + buffer.size();
    }

    const char* getVHeader() const
    {
        return &buffer[vheader];
    }

    void complete()
    {
        encodeLength();
    }

    void reset();

    static void getString(const char*& buff, uint16_t& len);

    Type type() const
    {
        return state == Complete ? static_cast<Type>(buffer[0] & 0xF0) : Unknown;
    }

    uint8_t flags() const
    {
        return static_cast<uint8_t>(buffer[0] & 0x0F);
    }

    void create(Type type)
    {
        buffer = (decltype(buffer)::value_type)type;
        buffer += '\0'; // reserved for msg length byte 1/2
        buffer += '\0'; // reserved for msg length byte 2/2 (fixed)
        vheader = 3;    // Should never change
        size    = 0;
        state   = Create;
    }

    InternalMqttError sendTo(InternalMqttClient*);

    void hexdump(const char* prefix = nullptr) const;

  private:
    void encodeLength();

    string   buffer;
    uint8_t  vheader;
    uint16_t size; // bytes left to receive
    State    state;
};

class InternalMqttBroker;

class InternalMqttClient
{
    enum Flags : uint8_t
    {
        FlagUserName     = 128,
        FlagPassword     = 64,
        FlagWillRetain   = 32,     // unsupported
        FlagWillQos      = 16 | 8, // unsupported
        FlagWill         = 4,      // unsupported
        FlagCleanSession = 2,      // unsupported
        FlagReserved     = 1
    };

    enum CltFlags : uint8_t
    {
        CltFlagNone      = 0,
        CltFlagConnected = 1,
        CltFlagToDelete  = 2
    };

  public:
    using CallBack = void (*)(const InternalMqttClient* source, const InternalTopic& topic, const char* payload, size_t payload_length);

    /** Constructor. Broker is the adress of a local broker if not null
        If you want to connect elsewhere, leave broker null and use connect() **/
    InternalMqttClient(InternalMqttBroker* broker, const string& id);

    InternalMqttClient(const string& id) : InternalMqttClient(nullptr, id)
    {
    }

    ~InternalMqttClient();

    void connect(InternalMqttBroker* local_broker);

    void connect(string broker, uint16_t port, uint16_t keep_alive = 10);

    bool connected()
    {
        return (localBroker != nullptr and tcpClient == nullptr) or (tcpClient and tcpClient->connected());
    }

    void write(const char* buf, size_t length)
    {
        if (tcpClient != nullptr)
        {
            tcpClient->write(buf, length);
        }
    }

    const string& id() const
    {
        return clientId;
    }

    void id(const string& new_id)
    {
        clientId = new_id;
    }

    void loop();

    void close(bool bSendDisconnect = true);

    void setCallback(CallBack cb)
    {
        callback = cb;
    };

    InternalMqttError publish(const InternalTopic&, const char* payload, size_t pay_length);

    InternalMqttError publish(const InternalTopic& t, const char* payload)
    {
        return publish(t, payload, strlen(payload));
    }

    InternalMqttError publish(const InternalTopic& t, const String& s)
    {
        return publish(t, s.c_str(), s.length());
    }

    InternalMqttError publish(const InternalTopic& t, const string& s)
    {
        return publish(t, s.c_str(), s.length());
    }

    InternalMqttError publish(const InternalTopic& t)
    {
        return publish(t, nullptr, 0);
    };

    InternalMqttError publish(const IotZoo::TopicLink& topicLink)
    {
        return publish(topicLink.TargetTopic.c_str(), topicLink.Payload.c_str(), topicLink.Payload.length());
    }

    InternalMqttError subscribe(InternalTopic topic, uint8_t qos = 0);

    InternalMqttError unsubscribe(InternalTopic topic);

    bool isSubscribedTo(const InternalTopic& topic) const;

    // connected to local broker
    bool isLocal() const
    {
        return tcpClient == nullptr;
    }

    void dump(string indent = "")
    {
        (void)indent;
#if USE_DEBUG_MESSAGES
        uint32_t ms = millis();
        Console << indent << "+-- " << '\'' << clientId.c_str() << "' " << (connected() ? " ON " : " OFF");
        Console << ", alive=" << alive << '/' << ms << ", ka=" << keep_alive << ' ';
        if (tcpClient)
        {
            if (tcpClient->connected())
                Console << TinyConsole::green << "connected";
            else
                Console << TinyConsole::red << "disconnected";
            Console << TinyConsole::white;
        }
        if (subscriptions.size())
        {
            bool c = false;
            Console << " [";
            for (auto s : subscriptions)
            {
                if (c)
                {
                    Console << ", ";
                }
                Console << s.str().c_str();
                c = true;
            }
            Console << ']';
        }
        Console << TinyConsole::erase_to_end << _EndLineCode::endl;
#endif
    }

    uint32_t keepAlive() const
    {
        return keep_alive;
    }

  private:
    bool mqtt_connected() const
    {
        return cltFlags & CltFlagConnected;
    }
    void setFlag(CltFlags f)
    {
        cltFlags |= f;
    }
    void resetFlag(CltFlags f)
    {
        cltFlags &= ~f;
    }

    // event when tcp/ip link established (real or fake)
    static void onConnect(void* client_ptr, TcpClient*);

    InternalMqttError sendTopic(const InternalTopic& topic, MqttMessage::Type type, uint8_t qos);

    void resubscribe();

    friend class InternalMqttBroker;
    InternalMqttClient(InternalMqttBroker* local_broker, TcpClient* client);
    // republish a received publish if topic matches any in subscriptions
    InternalMqttError publishIfSubscribed(const InternalTopic& topic, MqttMessage& msg);

    void clientAlive(uint32_t more_seconds);
    void processMessage(MqttMessage* message);

    uint8_t     cltFlags = CltFlagNone;
    char        mqtt_flags;
    uint32_t    keep_alive = 30;
    uint32_t    alive;
    MqttMessage message;

    // connection to local broker, or link to the parent
    // when MqttBroker uses MqttClient for each external connection
    InternalMqttBroker* localBroker = nullptr;

    TcpClient*          tcpClient = nullptr; // connection to remote broker
    std::set<InternalTopic> subscriptions;
    string              clientId;
    CallBack            callback = nullptr;
};

class InternalMqttBroker
{
    enum State : uint8_t
    {
        Disconnected, // Also the initial state
        Connecting,   // connect and sends a fake publish to avoid circular cnx
        Connected,    // this->broker is connected and circular cnx avoided
    };

  public:
    InternalMqttBroker(uint16_t port);

    ~InternalMqttBroker();

    void loop();

    // Connect the broker to a parent broker.
    void connect(const string& host, uint16_t port);

    // returns true if connected to another broker
    bool connected() const
    {
        return state == Connected;
    }

    size_t clientsCount() const
    {
        return clients.size();
    }

    void dump(string indent = "")
    {
        for (auto client : clients)
            client->dump(indent);
    }

    const std::vector<InternalMqttClient*> getClients() const
    {
        return clients;
    }

  private:
    friend class InternalMqttClient;

    static void onClient(void*, TcpClient*);

    InternalMqttError publish(const InternalMqttClient* source, const InternalTopic& topic, MqttMessage& msg) const;

    InternalMqttError subscribe(const InternalTopic& topic, uint8_t qos);

    // For clients that are added not by the broker itself (local clients)
    void addClient(InternalMqttClient* client);
    void removeClient(InternalMqttClient* client);

    bool compareString(const char* good, const char* str, uint8_t str_len) const;
    std::vector<InternalMqttClient*> clients;

  private:

    InternalMqttClient* remoteBroker = nullptr;

    State state = Disconnected;
};

#endif // USE_INTERNAL_MQTT