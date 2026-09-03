import { describe, expect, it } from 'vitest';
import { resolveContentAssetUrl } from './api';

describe('resolveContentAssetUrl', () => {
    it('resolves content upload paths with or without a leading slash', () => {
        expect(resolveContentAssetUrl('/uploads/content/card.png')).toBe('http://localhost:5274/uploads/content/card.png');
        expect(resolveContentAssetUrl('uploads/content/card.png')).toBe('http://localhost:5274/uploads/content/card.png');
        expect(resolveContentAssetUrl('/api/content/assets/content/card.png')).toBe('http://localhost:5274/api/content/assets/content/card.png');
    });

    it('keeps external, blob, and data image URLs intact', () => {
        expect(resolveContentAssetUrl('https://example.com/card.png')).toBe('https://example.com/card.png');
        expect(resolveContentAssetUrl('blob:http://localhost/image-id')).toBe('blob:http://localhost/image-id');
        expect(resolveContentAssetUrl('data:image/png;base64,abc')).toBe('data:image/png;base64,abc');
    });
});
