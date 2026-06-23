# Protocol

Tai lieu mo ta message JSON gui qua TCP socket.
Tat ca client, drawing server va load balancer phai dung chung format nay.

## 1. TCP Framing

Moi message gui qua socket theo dang:

```text
<4 bytes length><UTF-8 JSON bytes>
```

- `length`: so byte cua JSON payload, int 32-bit.
- JSON encode bang UTF-8.
- Ben nhan doc dung `length` byte roi moi parse JSON.
- Khong gui nhieu JSON lien tiep bang cach noi chuoi khong co length prefix.
- Gioi han message hien tai: 8 MB de ho tro file/media chat nho qua base64.

## 2. Message Envelope Chung

Tat ca message co envelope:

```json
{
  "type": "LOGIN_REQUEST",
  "requestId": "9f5b65f0-0e71-4db3-8cb2-97d2f5e20d28",
  "token": "session-token-or-null",
  "roomId": "ABCD",
  "senderId": "user-id-or-server-id",
  "timestamp": "2026-05-25T10:30:00Z",
  "payload": {}
}
```

| Field | Bat buoc | Mo ta |
|---|---:|---|
| `type` | Co | Loai message. Dung enum trong `MessageType.cs`. |
| `requestId` | Co | UUID de match request/response va debug log. |
| `token` | Khong | Session token sau khi login. `null` voi signup/login/register server. |
| `roomId` | Khong | Ma phong. Bat buoc voi draw/chat/join/leave room. |
| `senderId` | Khong | User id, server id, hoac `null` neu chua login. |
| `timestamp` | Co | UTC ISO-8601 string. |
| `payload` | Co | Object chua du lieu rieng cua tung message. Neu khong co data thi `{}`. |

Quy uoc ten field:

- JSON field dung `camelCase`.
- `type` dung `SCREAMING_SNAKE_CASE`.
- `roomId` dung 4-8 ky tu in hoa/so, vi du `ABCD`.
- `timestamp` luon UTC, ket thuc bang `Z`.

## 3. Response Chung

Moi request quan trong nen co response:

```json
{
  "type": "LOGIN_RESPONSE",
  "requestId": "same-as-request",
  "token": "new-session-token-or-null",
  "roomId": null,
  "senderId": "server",
  "timestamp": "2026-05-25T10:30:01Z",
  "payload": {
    "success": true,
    "message": "Login success",
    "data": {}
  }
}
```

Response payload chung:

| Field | Bat buoc | Mo ta |
|---|---:|---|
| `success` | Co | `true` neu xu ly thanh cong. |
| `message` | Co | Chuoi ngan de hien UI/debug. |
| `data` | Khong | Object ket qua rieng. |

## 4. Error Chung

Khi loi, server tra:

```json
{
  "type": "ERROR",
  "requestId": "same-as-request-if-any",
  "token": null,
  "roomId": null,
  "senderId": "server",
  "timestamp": "2026-05-25T10:30:01Z",
  "payload": {
    "code": "INVALID_TOKEN",
    "message": "Token is missing or expired"
  }
}
```

Error code nen dung:

- `INVALID_JSON`
- `UNKNOWN_MESSAGE_TYPE`
- `INVALID_PAYLOAD`
- `INVALID_TOKEN`
- `PERMISSION_DENIED`
- `ROOM_NOT_FOUND`
- `SERVER_UNAVAILABLE`
- `INTERNAL_ERROR`
- `...`

## 5. Message Types

### Auth

| Type | Huong | Payload |
|---|---|---|
| `SIGNUP_REQUEST` | Client -> Server | `username`, `password`, `displayName` |
| `SIGNUP_RESPONSE` | Server -> Client | response chung, `data.userId` |
| `LOGIN_REQUEST` | Client -> Server | `username`, `password` |
| `LOGIN_RESPONSE` | Server -> Client | response chung, `data.user` va `token` |
| `LOGOUT_REQUEST` | Client -> Server | `{}` |
| `LOGOUT_RESPONSE` | Server -> Client | response chung |

Example `LOGIN_REQUEST`:

```json
{
  "type": "LOGIN_REQUEST",
  "requestId": "9f5b65f0-0e71-4db3-8cb2-97d2f5e20d28",
  "token": null,
  "roomId": null,
  "senderId": null,
  "timestamp": "2026-05-25T10:30:00Z",
  "payload": {
    "username": "alice",
    "password": "123456"
  }
}
```

### Room

| Type | Huong | Payload |
|---|---|---|
| `CREATE_ROOM_REQUEST` | Client -> Server | `roomName` |
| `CREATE_ROOM_RESPONSE` | Server -> Client | response chung, `data.room` |
| `JOIN_ROOM_REQUEST` | Client -> Server | `roomId` |
| `JOIN_ROOM_RESPONSE` | Server -> Client | response chung, `data.room`, `data.drawHistory` |
| `LEAVE_ROOM_REQUEST` | Client -> Server | `roomId` |
| `LEAVE_ROOM_RESPONSE` | Server -> Client | response chung |
| `LIST_ROOMS_REQUEST` | Client -> Server | `{}` |
| `LIST_ROOMS_RESPONSE` | Server -> Client | response chung, `data.rooms` |
| `USER_JOINED` | Server -> Client | `user`, `roomId` |
| `USER_LEFT` | Server -> Client | `userId`, `roomId` |

### Drawing

| Type | Huong | Payload |
|---|---|---|
| `DRAW_STROKE` | Client -> Server, Server -> Client | stroke object |
| `DRAW_SHAPE` | Client -> Server, Server -> Client | shape/stroke object voi `tool` la shape |
| `UNDO_REQUEST` | Client -> Server | `roomId`, `targetStrokeId` optional |
| `UNDO_EVENT` | Server -> Client | `roomId`, `strokeId` |
| `CLEAR_CANVAS_REQUEST` | Client -> Server | `roomId` |
| `CLEAR_CANVAS_EVENT` | Server -> Client | `roomId` |
| `CANVAS_SYNC` | Server -> Client | `roomId`, `drawHistory` |

Stroke payload:

```json
{
  "strokeId": "stroke-001",
  "roomId": "ABCD",
  "userId": "user-001",
  "tool": "PEN",
  "color": "#FF0000",
  "thickness": 4,
  "points": [
    { "x": 120, "y": 80 },
    { "x": 122, "y": 84 }
  ]
}
```

Tool values:

- `PEN`
- `ERASER`
- `LINE`
- `RECTANGLE`
- `ELLIPSE`

### Chat

| Type | Huong | Payload |
|---|---|---|
| `CHAT_SEND` | Client -> Server | `roomId`, `content` |
| `CHAT_MESSAGE` | Server -> Client | `messageId`, `roomId`, `senderId`, `senderName`, `content`, `sentAt` |
| `CHAT_FILE_SEND` | Client -> Server | `roomId`, `content`, `attachment` |
| `CHAT_FILE_MESSAGE` | Server -> Client | `messageId`, `roomId`, `senderId`, `senderName`, `content`, `contentType`, `attachment`, `sentAt` |

File/media chat dung attachment base64 de demo don gian:

```json
{
  "messageId": "msg-001",
  "roomId": "ABCD",
  "senderId": "user-001",
  "senderName": "Alice",
  "content": "file demo",
  "contentType": "IMAGE",
  "attachment": {
    "attachmentId": "att-001",
    "fileName": "sketch.png",
    "contentType": "image/png",
    "size": 12345,
    "base64Data": "..."
  },
  "sentAt": "2026-06-08T10:30:00Z"
}
```

### Load Balancer

| Type | Huong | Payload |
|---|---|---|
| `SERVER_REGISTER` | DrawingServer -> LoadBalancer | `serverId`, `host`, `port`, `clientCount` |
| `SERVER_LOAD_UPDATE` | DrawingServer -> LoadBalancer | `serverId`, `clientCount` |
| `SERVER_HEARTBEAT` | DrawingServer -> LoadBalancer | `serverId` |
| `REQUEST_SERVER` | Client -> LoadBalancer | `{}` |
| `SERVER_ASSIGNED` | LoadBalancer -> Client | `serverId`, `host`, `port`, `clientCount` |

Example `REQUEST_SERVER`:

```json
{
  "type": "REQUEST_SERVER",
  "requestId": "30f0c935-23d0-4bd2-9da9-bd6300408a4f",
  "token": null,
  "roomId": null,
  "senderId": null,
  "timestamp": "2026-05-25T10:31:00Z",
  "payload": {}
}
```

## 6. Validation Rules

- Message thieu `type`, `requestId`, `timestamp`, `payload` => `INVALID_PAYLOAD`.
- `type` khong nam trong enum => `UNKNOWN_MESSAGE_TYPE`.
- Message can login ma thieu/sai `token` => `INVALID_TOKEN`.
- Message thao tac phong ma user khong thuoc phong => `PERMISSION_DENIED`.
- Draw payload phai co `roomId`, `strokeId`, `tool`, `points`.
- Chat `content` khong rong va nen gioi han do dai, vi du <= 500 ky tu.

## 7. Ma Hoa Payload

Neu lam phan Cryptography:

- Envelope van giu `type`, `requestId`, `timestamp`.
- `payload` co the duoc thay bang:

```json
{
  "encrypted": true,
  "iv": "base64-iv",
  "cipherText": "base64-cipher-text"
}
```

- Ben nhan giai ma payload truoc, sau do validate theo `type`.
