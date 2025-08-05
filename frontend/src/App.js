
import React, { useState, useRef } from 'react';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import './App.css';

const API_BASE_URL = process.env.REACT_APP_API_BASE_URL || 'http://localhost:5103';

function App() {
  const [messages, setMessages] = useState([]);
  const [input, setInput] = useState('');
  const [loading, setLoading] = useState(false);
  const eventSourceRef = useRef(null);

  const handleSend = async (e) => {
    e.preventDefault();
    if (!input.trim()) return;

    const userMessage = { role: 'user', content: input };
    setMessages((prev) => [...prev, userMessage]);
    setInput('');
    setLoading(true);

    // Prepare the request body to match the backend API
    const history = messages
      .filter((msg) => msg.role && msg.content)
      .map((msg) => ({ role: msg.role === 'user' ? 'User' : 'Assistant', content: msg.content }));
    const body = JSON.stringify({
      message: input,
      history: history,
      client: 'MCP',
    });

    // Use EventSource for streaming
    if (eventSourceRef.current) {
      eventSourceRef.current.close();
    }

    // Use fetch to POST, then open EventSource to stream
    // But since EventSource only supports GET, we'll use fetch for POST and then poll, or use fetch with ReadableStream (modern browsers)
    // Here, we'll use fetch with ReadableStream for simplicity
    try {
  const response = await fetch(`${API_BASE_URL}/api/Monitor/chat/stream`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'accept': '*/*' },
        body,
      });
      if (!response.body) throw new Error('No response body');
      const reader = response.body.getReader();
      let assistantMsg = '';
      while (true) {
        const { value, done } = await reader.read();
        if (done) break;
        const chunk = new TextDecoder().decode(value);
        // Handle both SSE (with 'data:') and plain streaming (no 'data:')
        const lines = chunk.includes('data:') ? chunk.split('data:') : [chunk];
        // eslint-disable-next-line no-loop-func
        lines.forEach((line) => {
          const trimmed = line.trim();
          if (trimmed) {
            try {
              // Try to parse as JSON, fallback to string
              const obj = JSON.parse(trimmed);
              if (obj && obj.content) {
                assistantMsg += obj.content;
                setMessages((prev) => {
                  // Replace last assistant message or add new
                  if (prev.length && prev[prev.length - 1].role === 'assistant') {
                    return [...prev.slice(0, -1), { role: 'assistant', content: assistantMsg }];
                  } else {
                    return [...prev, { role: 'assistant', content: assistantMsg }];
                  }
                });
              }
            } catch {
              // If not JSON, just append as text
              assistantMsg += trimmed;
              setMessages((prev) => {
                if (prev.length && prev[prev.length - 1].role === 'assistant') {
                  return [...prev.slice(0, -1), { role: 'assistant', content: assistantMsg }];
                } else {
                  return [...prev, { role: 'assistant', content: assistantMsg }];
                }
              });
            }
          }
        });
      }
    } catch (err) {
      setMessages((prev) => [...prev, { role: 'assistant', content: 'Error: ' + err.message }]);
    } finally {
      setLoading(false);
    }
  };


  return (
    <div className="App" style={{ minHeight: '100vh', background: '#181818', color: '#fff', display: 'flex', flexDirection: 'column', justifyContent: 'center', alignItems: 'center' }}>
      <h2 style={{ marginTop: 24 }}>Chat with MCP Agent</h2>
      <div className="chat-window" style={{ width: '100vw', maxWidth: 900, height: '70vh', background: '#23272f', color: '#fff', padding: 24, borderRadius: 12, minHeight: 400, boxShadow: '0 4px 32px #0008', display: 'flex', flexDirection: 'column', justifyContent: 'flex-end' }}>
        <div style={{ flex: 1, overflowY: 'auto', marginBottom: 16 }}>
          {messages.map((msg, idx) => (
            <div key={idx} style={{
              textAlign: msg.role === 'user' ? 'right' : 'left',
              margin: '16px 0',
              display: 'flex',
              flexDirection: msg.role === 'user' ? 'row-reverse' : 'row',
              alignItems: 'flex-start',
            }}>
              <div style={{
                background: msg.role === 'user' ? '#61dafb' : '#2d2d2d',
                color: msg.role === 'user' ? '#222' : '#90ee90',
                borderRadius: 12,
                padding: '12px 18px',
                maxWidth: '70%',
                wordBreak: 'break-word',
                fontSize: 16,
                boxShadow: msg.role === 'user' ? '0 2px 8px #61dafb44' : '0 2px 8px #0004',
              }}>
                <span style={{ fontWeight: 'bold', fontSize: 13 }}>{msg.role === 'user' ? 'You' : 'Agent'}:</span>
                <div style={{ marginTop: 6 }}>
                  {msg.role === 'assistant' ? (
                    <ReactMarkdown remarkPlugins={[remarkGfm]}>{msg.content}</ReactMarkdown>
                  ) : (
                    <span>{msg.content}</span>
                  )}
                </div>
              </div>
            </div>
          ))}
          {loading && <div style={{ color: '#aaa', margin: '8px 0' }}>Agent is typing...</div>}
        </div>
        <form onSubmit={handleSend} style={{ display: 'flex', gap: 12, marginTop: 8 }}>
          <input
            type="text"
            value={input}
            onChange={(e) => setInput(e.target.value)}
            placeholder="Type your message..."
            style={{ flex: 1, padding: 14, borderRadius: 8, border: '1px solid #444', background: '#333', color: '#fff', fontSize: 16 }}
            disabled={loading}
            autoFocus
          />
          <button type="submit" disabled={loading || !input.trim()} style={{ padding: '12px 28px', borderRadius: 8, background: '#61dafb', color: '#222', border: 'none', fontWeight: 'bold', fontSize: 16 }}>
            Send
          </button>
        </form>
      </div>
    </div>
  );
}

export default App;
