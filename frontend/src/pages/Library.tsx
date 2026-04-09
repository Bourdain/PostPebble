import React, { useState, useEffect } from 'react';
import { usePostPebble } from '../contexts/PostPebbleContext';
import { UploadCloud, CheckCircle2 } from 'lucide-react';
import { motion } from 'framer-motion';

export function Library() {
  const { mediaAssets, loadMedia, uploadMedia, apiBaseUrl } = usePostPebble();
  const [isUploading, setIsUploading] = useState(false);
  const [selectedIds, setSelectedIds] = useState<string[]>([]);

  useEffect(() => {
    loadMedia();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    setIsUploading(true);
    await uploadMedia(file);
    setIsUploading(false);
  };

  const toggleSelect = (id: string) => {
    setSelectedIds(prev => prev.includes(id) ? prev.filter(x => x !== id) : [...prev, id]);
  };

  return (
    <div>
      <div className="flex justify-between items-center mb-8">
        <div>
          <h2>Media Library</h2>
          <p className="text-sm">Manage your digital assets for cross-posting.</p>
        </div>
      </div>

      <div className="grid" style={{ gridTemplateColumns: 'minmax(300px, 1fr) 300px', gap: '2rem', maxWidth: '100%' }}>
        <div>
          <div className="masonry-grid">
            {mediaAssets.map((asset) => {
              const isSelected = selectedIds.includes(asset.id);
              return (
                <div 
                  key={asset.id} 
                  className={`asset-card ${isSelected ? 'selected' : ''}`}
                  onClick={() => toggleSelect(asset.id)}
                >
                  {asset.contentType.startsWith('image/') ? (
                    <img src={`${apiBaseUrl}${asset.publicUrl}`} alt={asset.originalFileName} />
                  ) : (
                    <div className="flex items-center justify-center p-4 text-center h-full">
                      {asset.originalFileName}
                    </div>
                  )}
                  {isSelected && (
                    <div style={{ position: 'absolute', top: '0.5rem', right: '0.5rem', zIndex: 10 }}>
                      <CheckCircle2 color="var(--highlight-blue)" fill="#fff" size={24} />
                    </div>
                  )}
                </div>
              );
            })}
          </div>
          {mediaAssets.length === 0 && (
            <div className="mt-6 text-center text-sm">
              <p>No media uploaded yet.</p>
            </div>
          )}
        </div>

        <div>
          <div className="card" style={{ position: 'sticky', top: '2rem' }}>
            <h3>Upload New</h3>
            <label className="upload-zone mt-4" style={{ cursor: 'pointer' }}>
              <input type="file" onChange={handleFileChange} disabled={isUploading} />
              <UploadCloud size={48} color={isUploading ? 'var(--text-secondary)' : 'var(--highlight-blue)'} />
              <div className="text-center">
                {isUploading ? (
                  <motion.div animate={{ opacity: [0.5, 1, 0.5] }} transition={{ repeat: Infinity }}>
                    Uploading...
                  </motion.div>
                ) : (
                  <>
                    <p style={{ margin: '0 0 0.5rem' }}>Drag & drop or click</p>
                    <small style={{ color: 'var(--text-secondary)' }}>Supports Images & Videos</small>
                  </>
                )}
              </div>
            </label>

            {selectedIds.length > 0 && (
              <div className="mt-6">
                <h4>Selected ({selectedIds.length})</h4>
                <button 
                  className="btn-secondary w-full mt-2"
                  onClick={() => setSelectedIds([])}
                >
                  Clear Selection
                </button>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
