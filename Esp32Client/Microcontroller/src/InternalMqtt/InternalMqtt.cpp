#include "./InternalMqtt/InternalMqtt.h"
#include "Defines.hpp"

#ifdef USE_INTERNAL_MQTT

#include "DebugHelper.hpp"
#include <sstream>

InternalMqttBroker::InternalMqttBroker(uint16_t port)
{
}

InternalMqttBroker::~InternalMqttBroker()
{
    while (clients.size())
    {
        auto client         = clients[0];
        client->localBroker = nullptr;
        if (client->cltFlags & InternalMqttClient::CltFlags::CltFlagToDelete)
        {
            delete client;
        }
        clients.erase(clients.begin());
    }
}

InternalMqttClient::InternalMqttClient(InternalMqttBroker* internalMqttBroker, const string& id) : localBroker(internalMqttBroker), clientId(id)
{
    alive      = 0;
    keep_alive = 0;

    if (nullptr != internalMqttBroker)
    {
        internalMqttBroker->addClient(this);
    }
}

InternalMqttClient::~InternalMqttClient()
{
    close();
    delete tcpClient;
    debug("*** MqttClient delete()");
}

void InternalMqttClient::close(bool bSendDisconnect)
{
    debug("close " << id().c_str());
    resetFlag(CltFlagConnected);
    if (nullptr != tcpClient) // connected to a remote broker
    {
        if (bSendDisconnect and tcpClient->connected())
        {
            message.create(MqttMessage::Type::Disconnect);
            message.hexdump("close");
            message.sendTo(this);
        }
        tcpClient->stop();
    }

    if (localBroker)
    {
        localBroker->removeClient(this);
        localBroker = nullptr;
    }
}

void InternalMqttClient::connect(InternalMqttBroker* local)
{
    debug("MqttClient::connect_local");
    close();
    localBroker = local;
    localBroker->addClient(this);
}

void InternalMqttClient::connect(string broker, uint16_t port, uint16_t ka)
{
    debug("MqttClient::connect_to_host " << broker << ':' << port);
    keep_alive = ka;
    close();

    delete tcpClient;
    tcpClient = new TcpClient;

    if (tcpClient->connect(broker.c_str(), port))
    {
        debug("link established");
        onConnect(this, tcpClient);
    }
    else
    {
        debug("unable to connect.");
    }
}

void InternalMqttBroker::addClient(InternalMqttClient* client)
{
    debug("MqttBroker::addClient");
    clients.push_back(client);
}

void InternalMqttBroker::connect(const string& host, uint16_t port)
{
    debug("MqttBroker::connect fixme id");
    if (remoteBroker == nullptr)
    {
        remoteBroker = new InternalMqttClient(nullptr, "id");
    }
    remoteBroker->connect(host, port);
    remoteBroker->localBroker = this; // Because connect removed the link
}

void InternalMqttBroker::removeClient(InternalMqttClient* remove)
{
    debug("removeClient");
    for (auto it = clients.begin(); it != clients.end(); it++)
    {
        auto client = *it;
        if (client == remove)
        {
            // TODO if this broker is connected to an external broker
            // we have to unsubscribe remove's topics.
            // (but doing this, check that other clients are not subscribed...)
            // Unless -> we could receive useless messages
            //        -> we are using (memory) one IndexedString plus its string for nothing.
            debug("Remove " << clients.size());
            clients.erase(it);
            debug("Client removed " << clients.size());
            return;
        }
    }
    debug("Error cannot remove client");
}

void InternalMqttBroker::onClient(void* broker_ptr, TcpClient* client)
{
    debug("MqttBroker::onClient");
    InternalMqttBroker* broker = static_cast<InternalMqttBroker*>(broker_ptr);

    InternalMqttClient* mqtt = new InternalMqttClient(broker, client);
    mqtt->setFlag(InternalMqttClient::CltFlags::CltFlagToDelete);
    broker->addClient(mqtt);
    debug("New client");
}

void InternalMqttBroker::loop()
{
    if (nullptr != remoteBroker)
    {
        // TODO should monitor broker's activity.
        // 1 When broker disconnect and reconnect we have to re-subscribe
        remoteBroker->loop();
    }

    for (size_t i = 0; i < clients.size(); i++)
    {
        InternalMqttClient* client = clients[i];
        if (client->connected())
        {
            client->loop();
        }
        else
        {
            debug("Client " << client->id().c_str() << " Disconnected, local_broker=" << (dbg_ptr)client->localBroker);
            // Note: deleting a client not added by the broker itself will probably crash later.
            delete client;
            break;
        }
    }
}

InternalMqttError InternalMqttBroker::subscribe(const InternalTopic& topic, uint8_t qos)
{
    debug("InternalMqttBroker::subscribe");

    if (remoteBroker && remoteBroker->connected())
    {
        return remoteBroker->subscribe(topic, qos);
    }
    return MqttNowhereToSend;
}

InternalMqttError InternalMqttBroker::publish(const InternalMqttClient* source, const InternalTopic& topic, MqttMessage& msg) const
{
    InternalMqttError retval = MqttOk;

    debug("MqttBroker::publish");
    int i = 0;
    for (auto client : clients)
    {
        i++;

        debug("broker:" << (remoteBroker && remoteBroker->connected() ? "linked" : "alone")
                << "  srce=" << (source->isLocal() ? "loc" : "rem") << " clt#" << i << ", local=" << client->isLocal()
                << ", con=" << client->connected() << endl);

        bool doit = false;
        if (remoteBroker && remoteBroker->connected()) // this (MqttBroker) is connected (to a external broker)
        {
            // ext_broker -> clients or clients -> ext_broker
            if (source == remoteBroker) // external broker -> internal clients
            {
                doit = true;
            }
            else // external clients -> this broker
            {
                // As this broker is connected to another broker, simply forward the msg
                InternalMqttError ret = remoteBroker->publishIfSubscribed(topic, msg);
                if (ret != MqttOk)
                {
                    retval = ret;
                }
            }
        }
        else // Disconnected
        {
            doit = true;
        }
        if (doit)
        {
            retval = client->publishIfSubscribed(topic, msg);
        }

    }
    return retval;
}

bool InternalMqttBroker::compareString(const char* good, const char* str, uint8_t len) const
{
    while (len-- and *good++ == *str++)
        ;

    return *good == 0;
}

void MqttMessage::getString(const char*& buff, uint16_t& len)
{
    len = getSize(buff);
    buff += 2;
}

void InternalMqttClient::clientAlive(uint32_t more_seconds)
{
    debug("MqttClient::clientAlive");
    if (keep_alive)
    {
        alive = millis() + 1000 * (keep_alive + more_seconds);
    }
    else
    {
        alive = 0;
    }
}

void InternalMqttClient::loop()
{
    if (keep_alive && (millis() >= alive))
    {
        if (nullptr != localBroker)
        {
            debug("timeout client");
            close();
            debug("closed");
        }
        else if (tcpClient && tcpClient->connected())
        {
            debug("pingreq");
            uint16_t pingreq = MqttMessage::Type::PingReq;
            tcpClient->write((const char*)(&pingreq), 2);
            clientAlive(0);
        }
    }

    while (tcpClient && tcpClient->available() > 0)
    {
        message.incoming(tcpClient->read());
        if (message.type())
        {
            processMessage(&message);
            message.reset();
        }
    }
}

void InternalMqttClient::onConnect(void* mqttclient_ptr, TcpClient*)
{
    InternalMqttClient* mqtt = static_cast<InternalMqttClient*>(mqttclient_ptr);
    debug("MqttClient::onConnect");
    MqttMessage msg(MqttMessage::Type::Connect);
    msg.add("MQTT", 4);
    msg.add(0x4); // Mqtt protocol version 3.1.1
    msg.add(0x0); // Connect flags         TODO user / name

    msg.add((char)(mqtt->keep_alive >> 8)); // keep_alive
    msg.add((char)(mqtt->keep_alive & 0xFF));
    msg.add(mqtt->clientId);
    debug("cnx: mqtt connecting");
    msg.sendTo(mqtt);
    msg.reset();
    debug("cnx: mqtt sent " << (dbg_ptr)mqtt->localBroker);

    mqtt->clientAlive(0);
}

void InternalMqttClient::resubscribe()
{
    // TODO resubscription limited to 256 bytes
    if (subscriptions.size())
    {
        MqttMessage msg(MqttMessage::Type::Subscribe, 2);

        // TODO manage packet identifier
        msg.add(0);
        msg.add(0);

        for (auto topic : subscriptions)
        {
            msg.add(topic);
            msg.add(0); // TODO qos
        }
        msg.sendTo(this); // TODO return value
    }
}

InternalMqttError InternalMqttClient::subscribe(InternalTopic topic, uint8_t qos)
{
    debug("Subscribing to internal topic " + String(topic.c_str()));
    InternalMqttError ret = MqttOk;

    subscriptions.insert(topic);

    if (localBroker == nullptr) // remote broker
    {
        debug("Subscribing to remote topic " + String(topic.c_str()));
        return sendTopic(topic, MqttMessage::Type::Subscribe, qos);
    }
    else
    {
        debug("Subscribing to local topic " + String(topic.c_str()));
        return localBroker->subscribe(topic, qos);
    }
    return ret;
}

InternalMqttError InternalMqttClient::unsubscribe(InternalTopic topic)
{
    debug("MqttClient::unsubscribe");
    auto it = subscriptions.find(topic);
    if (it != subscriptions.end())
    {
        subscriptions.erase(it);
        if (localBroker == nullptr) // remote broker
        {
            return sendTopic(topic, MqttMessage::Type::UnSubscribe, 0);
        }
    }
    return MqttOk;
}

InternalMqttError InternalMqttClient::sendTopic(const InternalTopic& topic, MqttMessage::Type type, uint8_t qos)
{
    debug("MqttClient::sendTopic");
    MqttMessage msg(type, 2);

    // TODO manage packet identifier
    msg.add(0);
    msg.add(0);

    msg.add(topic);
    msg.add(qos);

    // TODO instead we should wait (state machine) for SUBACK / UNSUBACK?
    return msg.sendTo(this);
}

void InternalMqttClient::processMessage(MqttMessage* mesg)
{
#if USE_DEBUG_MESSAGES
    mesg->hexdump("Incoming");
#endif
    auto        header = mesg->getVHeader();
    const char* payload;
    uint16_t    len;
    bool        bclose = true;

    switch (mesg->type())
    {
    case MqttMessage::Type::Connect:
        if (mqtt_connected())
        {
            debug("already connected");
            break;
        }
        payload    = header + 10;
        mqtt_flags = header[7];
        keep_alive = MqttMessage::getSize(header + 8);
        if (strncmp("MQTT", header + 2, 4))
        {
            debug("bad mqtt header");
            break;
        }
        if (header[6] != 0x04)
        {
            debug("Unsupported MQTT version (" << (int)header[6] << "), only version=4 supported" << endl);
            break; // Level 3.1.1
        }

        // ClientId
        mesg->getString(payload, len);
        clientId = string(payload, len);
        payload += len;

        if (mqtt_flags & FlagWill) // Will topic
        {
            mesg->getString(payload, len); // Will Topic
            payload += len;

            mesg->getString(payload, len); // Will Message
            payload += len;
        }
        // FIXME forgetting credential is allowed (security hole)
        if (mqtt_flags & FlagUserName)
        {
            mesg->getString(payload, len);

            payload += len;
        }
        if (mqtt_flags & FlagPassword)
        {
            mesg->getString(payload, len);

            payload += len;
        }

        debug("Client " << clientId << " connected : keep alive=" << keep_alive << '.' << endl);

        bclose = false;
        setFlag(CltFlagConnected);
        {
            MqttMessage msg(MqttMessage::Type::ConnAck);
            msg.add(0); // Session present (not implemented)
            msg.add(0); // Connection accepted
            msg.sendTo(this);
        }
        break;

    case MqttMessage::Type::ConnAck:
        setFlag(CltFlagConnected);
        bclose = false;
        resubscribe();
        break;

    case MqttMessage::Type::SubAck:
    case MqttMessage::Type::PubAck:
        if (not mqtt_connected())
            break;
        // Ignore acks
        bclose = false;
        break;

    case MqttMessage::Type::PingResp:
        // TODO: no PingResp is suspicious (server dead)
        bclose = false;
        break;

    case MqttMessage::Type::PingReq:
        if (not mqtt_connected())
            break;
        if (tcpClient)
        {
            uint16_t pingreq = MqttMessage::Type::PingResp;
            debug("Ping response to client");
            tcpClient->write((const char*)(&pingreq), 2);
            bclose = false;
        }
        else
        {
            debug("internal pingreq?");
        }
        break;

    case MqttMessage::Type::Subscribe:
    case MqttMessage::Type::UnSubscribe:
    {
        if (not mqtt_connected())
            break;
        payload = header + 2;

        debug("un/subscribe loop");
        string qoss;
        while (payload < mesg->end())
        {
            mesg->getString(payload, len); // Topic
            debug("  topic (" << string(payload, len) << ')');
            // subscribe(Topic(payload, len));
            InternalTopic topic(payload, len);

            payload += len;
            if (mesg->type() == MqttMessage::Type::Subscribe)
            {
                uint8_t qos = *payload++;
                if (qos != 0)
                {
                    debug("Unsupported QOS" << qos << endl);
                    qoss.push_back(0x80);
                }
                else
                    qoss.push_back(qos);
                subscriptions.insert(topic);
            }
            else
            {
                auto it = subscriptions.find(topic);
                if (it != subscriptions.end())
                    subscriptions.erase(it);
            }
        }
        debug("end loop");
        bclose = false;

        MqttMessage ack(mesg->type() == MqttMessage::Type::Subscribe ? MqttMessage::Type::SubAck : MqttMessage::Type::UnSuback);
        ack.add(header[0]);
        ack.add(header[1]);
        ack.add(qoss.c_str(), qoss.size(), false);
        ack.sendTo(this);
    }
    break;

    case MqttMessage::Type::UnSuback:
        if (not mqtt_connected())
            break;
        bclose = false;
        break;

    case MqttMessage::Type::Publish:
#if USE_DEBUG_MESSAGES
        Console << "publish " << mqtt_connected() << '/' << (long)tcpClient << endl;
#endif
        if (mqtt_connected() or tcpClient == nullptr)
        {
            uint8_t qos = mesg->flags();
            payload     = header;
            mesg->getString(payload, len);
            InternalTopic published(payload, len);
            payload += len;
#if USE_DEBUG_MESSAGES
            Console << "Received Publish (" << published.str().c_str() << ") size=" << (int)len << endl;
#endif
            // << '(' << string(payload, len).c_str() << ')'  << " msglen=" << mesg->length() << endl;
            if (qos)
                payload += 2; // ignore packet identifier if any
            len = mesg->end() - payload;
            // TODO reset DUP
            // TODO reset RETAIN

            if (localBroker == nullptr or tcpClient == nullptr) // internal MqttClient receives publish
            {
#if USE_DEBUG_MESSAGES
                if (DebugHelper::debugLevel >= 2)
                {
                    Console << (isSubscribedTo(published) ? "not" : "") << " subscribed.\n";
                    Console << "has " << (callback ? "" : "no ") << " callback.\n";
                }
#endif
                if (callback and isSubscribedTo(published))
                {
                    callback(this, published, payload, len); // TODO send the real payload
                }
            }
            else if (localBroker) // from outside to inside
            {
                debug("publishing to local_broker");
                localBroker->publish(this, published, *mesg);
            }
            bclose = false;
        }
        break;

    case MqttMessage::Type::Disconnect:
        // TODO should discard any will msg
        if (not mqtt_connected())
            break;
        resetFlag(CltFlagConnected);
        close(false);
        bclose = false;
        break;

    default:
        bclose = true;
        break;
    };
    if (bclose)
    {
#if USE_DEBUG_MESSAGES
        debug("*************** Error msg 0x" << _HEX(mesg->type()));
        mesg->hexdump("------- ERROR -------");
        dump();

#endif
        close();
    }
    else
    {
        clientAlive(localBroker ? 5 : 0);
    }
}

bool InternalTopic::matches(const InternalTopic& topic) const
{
    if (getIndex() == topic.getIndex())
        return true;
    const char* p1 = c_str();
    const char* p2 = topic.c_str();

    if (p1 == p2)
        return true;
    if (*p2 == '$' and *p1 != '$')
        return false;

    while (*p1 and *p2)
    {
        if (*p1 == '+')
        {
            ++p1;
            if (*p1 and *p1 != '/')
                return false;
            if (*p1)
                ++p1;
            while (*p2 and *p2++ != '/')
                ;
        }
        else if (*p1 == '#')
        {
            if (*++p1 == 0)
                return true;
            return false;
        }
        else if (*p1 == '*')
        {
            const char c = *(p1 + 1);
            if (c == 0)
                return true;
            if (c != '/')
                return false;
            const char* p = p1 + 2;
            while (*p and *p2)
            {
                if (*p == *p2)
                {
                    if (*p == 0)
                        return true;
                    if (*p == '/')
                    {
                        p1 = p;
                        break;
                    }
                }
                else
                {
                    while (*p2 and *p2++ != '/')
                        ;
                    break;
                }
                ++p;
                ++p2;
            }
            if (*p == 0)
            {
                return true;
            }
        }
        else if (*p1 == *p2)
        {
            ++p1;
            ++p2;
        }
        else
            return false;
    }
    if (*p1 == '/' and p1[1] == '#' and p1[2] == 0)
        return true;
    return *p1 == 0 and *p2 == 0;
}

// publish from local client
InternalMqttError InternalMqttClient::publish(const InternalTopic& topic, const char* payload, size_t pay_length)
{
    debug("Publishing to internal MQTT topic: " + String(topic.c_str()) + ", payload: " + String(payload));

    MqttMessage msg(MqttMessage::Publish);
    msg.add(topic);
    msg.add(payload, pay_length, false);
    msg.complete();

    if (localBroker)
    {
        return localBroker->publish(this, topic, msg);
    }
    else if (tcpClient)
    {
        return msg.sendTo(this);
    }
    else
    {
        return MqttNowhereToSend;
    }
}

// republish a received publish if it matches any in subscriptions
InternalMqttError InternalMqttClient::publishIfSubscribed(const InternalTopic& topic, MqttMessage& msg)
{
    InternalMqttError retval = MqttOk;

    debug("mqttclient publishIfSubscribed topic: " << topic.c_str() << ", subscriptions: " << subscriptions.size());
    if (isSubscribedTo(topic))
    {
        if (tcpClient)
        {
            debug("Forwarding message for topic: " << topic.c_str() << " to external broker");
            retval = msg.sendTo(this);
        }
        else
        {
            debug("Processing message for topic: " << topic.c_str());
            processMessage(&msg);
        }
    }
    return retval;
}

bool InternalMqttClient::isSubscribedTo(const InternalTopic& topic) const
{
    for (const auto& subscription : subscriptions)
    {
        if (subscription.matches(topic))
        {
            debug("Found subscription for topic: " << topic.c_str());
            return true;
        }
    }
    debug("No subscription found for topic: " << topic.c_str());
    return false;
}

void MqttMessage::reset()
{
    buffer.clear();
    state = FixedHeader;
    size  = 0;
}

void MqttMessage::incoming(char in_byte)
{
    buffer += in_byte;
    switch (state)
    {
    case FixedHeader:
        size  = MaxBufferLength;
        state = Length;
        break;
    case Length:

        if (size == MaxBufferLength)
            size = in_byte & 0x7F;
        else
            size += static_cast<uint16_t>(in_byte & 0x7F) << 7;

        if (size > MaxBufferLength)
            state = Error;
        else if ((in_byte & 0x80) == 0)
        {
            vheader = buffer.length();
            if (size == 0)
                state = Complete;
            else
            {
                buffer.reserve(size);
                state = VariableHeader;
            }
        }
        break;
    case VariableHeader:
    case PayLoad:
        --size;
        if (size == 0)
        {
            state = Complete;
            // hexdump("rec");
        }
        break;
    case Create:
        size++;
        break;
    case Complete:
    default:
#if USE_DEBUG_MESSAGES
        debug("Spurious " << _HEX(in_byte) << endl);
        hexdump("spurious");
#endif
        reset();
        break;
    }
    if (buffer.length() > MaxBufferLength)
    {
        debug("Too long " << state);
        reset();
    }
}

void MqttMessage::add(const char* p, size_t len, bool addLength)
{
    if (addLength)
    {
        buffer.reserve(buffer.length() + 2);
        incoming(len >> 8);
        incoming(len & 0xFF);
    }
    while (len--)
        incoming(*p++);
}

void MqttMessage::encodeLength()
{
    debug("encodeLength");
    if (state != Complete)
    {
        int length = buffer.size() - 3; // 3 = 1 byte for header + 2 bytes for pre-reserved length field.
        if (length <= 0x7F)
        {
            buffer.erase(1, 1);
            buffer[1] = length;
            vheader   = 2;
        }
        else
        {
            buffer[1] = 0x80 | (length & 0x7F);
            buffer[2] = (length >> 7);
            vheader   = 3;
        }

        // We could check that buffer[2] < 128 (end of length encoding)
        state = Complete;
    }
};

InternalMqttError MqttMessage::sendTo(InternalMqttClient* client)
{
    if (buffer.size())
    {
        debug("sending " << buffer.size() << " bytes to " << client->id());
        encodeLength();
        hexdump("Sending ");
        client->write(&buffer[0], buffer.size());
    }
    else
    {
        debug("Invalid send?");
        return MqttInvalidMessage;
    }
    return MqttOk;
}

void MqttMessage::hexdump(const char* prefix) const
{
    (void)prefix;
#if USE_DEBUG_MESSAGES
    if (DebugHelper::debugLevel < 2)
        return;
    static std::map<Type, string> tts = {{Connect, "Connect"},     {ConnAck, "Connack"},   {Publish, "Publish"},         {PubAck, "Puback"},
                                         {Subscribe, "Subscribe"}, {SubAck, "Suback"},     {UnSubscribe, "Unsubscribe"}, {UnSuback, "Unsuback"},
                                         {PingReq, "Pingreq"},     {PingResp, "Pingresp"}, {Disconnect, "Disconnect"}};
    string                        t("Unknown");
    Type                          typ = static_cast<Type>(buffer[0] & 0xF0);
    if (tts.find(typ) != tts.end())
    {
        t = tts[typ];

    }
    debug("---> MESSAGE " << t << ' ' << _HEX(typ) << ' ' << " mem=" << ESP.getFreeHeap() << endl);

    uint16_t    addr          = 0;
    const int   bytes_per_row = 8;
    const char* hex_to_str    = " | ";
    const char* separator     = hex_to_str;
    const char* half_sep      = " - ";
    string      ascii;

    Console << prefix << " size(" << buffer.size() << "), state=" << state << endl;

    for (const char chr : buffer)
    {
        if ((addr % bytes_per_row) == 0)
        {
            if (ascii.length())
                Console << hex_to_str << ascii << separator << endl;
            if (prefix)
                Console << prefix << separator;
            ascii.clear();
        }
        addr++;
        if (chr < 16)
            Console << '0';
        Console << _HEX(chr) << ' ';

        ascii += (chr < 32 ? '.' : chr);
        if (ascii.length() == (bytes_per_row / 2))
            ascii += half_sep;
    }
    if (ascii.length())
    {
        while (ascii.length() < bytes_per_row + strlen(half_sep))
        {
            Console << "   "; // spaces per hexa byte
            ascii += ' ';
        }
        Console << hex_to_str << ascii << separator;
    }

    Console << endl;
#endif
}

#endif // USE_INTERNAL_MQTT