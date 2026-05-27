import React, { useState } from "react";

type DownloadResponse = {
  file_id: string;
  download_url: string;
};

const API_BASE = "http://localhost:9000";

export const DownloadPage: React.FC = () => {
  const [remoteUrl, setRemoteUrl] = useState("");
  const [downloadUrl, setDownloadUrl] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleDownload = async () => {
    setLoading(true);
    setError(null);
    setDownloadUrl(null);

    try {
      const res = await fetch(`${API_BASE}/api/download`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ url: remoteUrl }),
      });

      if (!res.ok) {
        throw new Error(`HTTP ${res.status}`);
      }

      const data: DownloadResponse = await res.json();
      setDownloadUrl(data.download_url);

      // Option 1: immediately trigger browser download
      window.location.href = data.download_url;

      // Option 2: or show a link:
      // setDownloadUrl(data.download_url);
    } catch (e: any) {
      setError(e.message ?? "Unknown error");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={{ padding: 24 }}>
      <h1>Remote File Downloader</h1>

      <label>
        Remote file URL:
        <input
          type="text"
          value={remoteUrl}
          onChange={(e) => setRemoteUrl(e.target.value)}
          style={{ width: "100%", marginTop: 8 }}
          placeholder="https://example.com/file.zip"
        />
      </label>

      <button
        onClick={handleDownload}
        disabled={loading || !remoteUrl}
        style={{ marginTop: 16 }}
      >
        {loading ? "Downloading..." : "Download via service"}
      </button>

      {error && (
        <div style={{ color: "red", marginTop: 16 }}>
          Error: {error}
        </div>
      )}

      {downloadUrl && (
        <div style={{ marginTop: 16 }}>
          <a href={downloadUrl} download>
            Click here if the download didn’t start
          </a>
        </div>
      )}
    </div>
  );
};

