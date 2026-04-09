import React, { useState, useEffect } from 'react';
import { usePostPebble } from '../contexts/PostPebbleContext';
import { UploadCloud, CheckCircle2 } from 'lucide-react';
import { motion } from 'framer-motion';

export function Library() {
  const { mediaAssets, loadMedia, uploadMedia, deleteMedia, updateMediaTags, apiBaseUrl } = usePostPebble();
  const [isUploading, setIsUploading] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);
  const [selectedIds, setSelectedIds] = useState<string[]>([]);
  const [uploadTags, setUploadTags] = useState("");
  const [searchQuery, setSearchQuery] = useState("");

  useEffect(() => {
    loadMedia();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    setIsUploading(true);
    await uploadMedia(file, uploadTags);
    setIsUploading(false);
    setUploadTags("");
  };

  const filteredAssets = mediaAssets.filter(asset => {
    if (!searchQuery) return true;
    const lowerQuery = searchQuery.toLowerCase();
    return asset.originalFileName.toLowerCase().includes(lowerQuery) || 
           asset.tags?.some(tag => tag.toLowerCase().includes(lowerQuery));
  });

  const toggleSelect = (id: string) => {
    setSelectedIds(prev => prev.includes(id) ? prev.filter(x => x !== id) : [...prev, id]);
  };

  const handleDeleteSelected = async () => {
    if (!window.confirm(`Are you sure you want to delete ${selectedIds.length} item(s)?`)) return;
    setIsDeleting(true);
    for (const id of selectedIds) {
      await deleteMedia(id);
    }
    setSelectedIds([]);
    setIsDeleting(false);
  };

  return (
    <div>
      <div className="flex justify-between items-center mb-8">
        <div>
          <h2>Media Library</h2>
          <p className="text-sm">Manage your digital assets for cross-posting.</p>
        </div>
        <div>
          <input 
            type="text" 
            placeholder="Search files or tags..." 
            className="input-field"
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
          />
        </div>
      </div>

      <div className="grid" style={{ gridTemplateColumns: 'minmax(300px, 1fr) 300px', gap: '2rem', maxWidth: '100%' }}>
        <div>
          <div className="masonry-grid">
            {filteredAssets.map((asset) => {
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
                  {asset.tags && asset.tags.length > 0 && (
                    <div style={{ position: 'absolute', bottom: '0.5rem', left: '0.5rem', display: 'flex', gap: '4px', flexWrap: 'wrap', zIndex: 10 }}>
                      {asset.tags.map(tag => (
                        <span key={tag} style={{ background: 'rgba(0,0,0,0.6)', color: '#fff', fontSize: '10px', padding: '2px 6px', borderRadius: '4px' }}>
                          #{tag.trim()}
                        </span>
                      ))}
                    </div>
                  )}
                </div>
              );
            })}
          </div>
          {filteredAssets.length === 0 && (
            <div className="mt-6 text-center text-sm">
              <p>No media found.</p>
            </div>
          )}
        </div>

        <div>
          <div className="card" style={{ position: 'sticky', top: '2rem' }}>
            <h3>Upload New</h3>
            <div className="mt-4">
              <input 
                type="text" 
                className="input-field" 
                placeholder="Initial tags (comma separated)" 
                value={uploadTags}
                onChange={(e) => setUploadTags(e.target.value)}
                disabled={isUploading}
              />
            </div>
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
                <div className="mt-2 text-sm text-center">
                   <input 
                     type="text"
                     placeholder="Add tag and press Enter..."
                     className="input-field mb-2"
                     disabled={isDeleting}
                     onKeyDown={async (e) => {
                       if (e.key === 'Enter' && e.currentTarget.value.trim()) {
                         setIsDeleting(true);
                         const newTag = e.currentTarget.value.trim();
                         for (const id of selectedIds) {
                           const asset = mediaAssets.find(x => x.id === id);
                           if (asset) {
                             const tags = Array.from(new Set([...(asset.tags || []), newTag]));
                             await updateMediaTags(id, tags);
                           }
                         }
                         e.currentTarget.value = "";
                         setIsDeleting(false);
                       }
                     }}
                   />
                </div>
                <div className="flex gap-2 mt-2">
                  <button 
                    className="btn-secondary flex-1"
                    onClick={() => setSelectedIds([])}
                    disabled={isDeleting}
                  >
                    Clear
                  </button>
                  <button 
                    className="btn-primary flex-1"
                    style={{ background: 'var(--status-failed, #ef4444)' }}
                    onClick={handleDeleteSelected}
                    disabled={isDeleting}
                  >
                    {isDeleting ? 'Working...' : 'Delete'}
                  </button>
                </div>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
