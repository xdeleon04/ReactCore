import React, { useMemo, useState } from 'react';

export const ImageGallery: React.FC<{
  imageUrls: string[];
  alt: string;
}> = ({ imageUrls, alt }) => {
  const urls = useMemo(() => imageUrls.filter(Boolean), [imageUrls]);
  const [activeIndex, setActiveIndex] = useState(0);

  const activeUrl = urls[activeIndex];

  if (urls.length === 0) {
    return <div className="aspect-[4/3] w-full rounded bg-gray-100" aria-label="No product images" />;
  }

  return (
    <div>
      <div className="aspect-[4/3] w-full overflow-hidden rounded bg-gray-100">
        <img src={activeUrl} alt={alt} className="h-full w-full object-cover" />
      </div>

      {urls.length > 1 ? (
        <div className="mt-3 flex gap-2 overflow-x-auto" aria-label="Image thumbnails">
          {urls.map((u, idx) => (
            <button
              key={`${u}-${idx}`}
              type="button"
              className={`h-14 w-20 flex-shrink-0 overflow-hidden rounded border ${
                idx === activeIndex ? 'border-gray-900' : 'border-gray-200'
              }`}
              onClick={() => setActiveIndex(idx)}
              aria-label={`View image ${idx + 1}`}
            >
              <img src={u} alt={alt} className="h-full w-full object-cover" />
            </button>
          ))}
        </div>
      ) : null}
    </div>
  );
};
