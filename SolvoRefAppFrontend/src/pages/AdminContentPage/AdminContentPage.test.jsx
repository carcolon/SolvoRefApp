import { render, screen, waitFor } from '@testing-library/react';
import { vi, describe, it, expect } from 'vitest';
import AdminContentPage from './AdminContentPage';

vi.mock('../../animations/usePageMotion', () => ({
    usePageMotion: () => undefined,
}));

vi.mock('gsap', () => ({
    gsap: {
        killTweensOf: () => undefined,
        set: () => undefined,
        to: () => undefined,
        fromTo: () => undefined,
    },
}));

vi.mock('../../components/AnimatedPageShell', () => ({
    default: ({ children, ...props }) => <div {...props}>{children}</div>,
}));

vi.mock('../../components/CardStudio/CardStudioCanvas', () => ({
    default: () => <div data-testid="card-studio-canvas">Canvas</div>,
}));

vi.mock('../../components/CardStudio/CardStudioInspector', () => ({
    default: () => <div data-testid="card-studio-inspector">Inspector</div>,
}));

vi.mock('../../components/HomePromoModal/HomePromoModal', () => ({
    default: () => <div data-testid="home-promo-modal">Modal</div>,
}));

vi.mock('../../services/contentService', () => ({
    fetchContentCards: vi.fn().mockResolvedValue({
        success: true,
        data: [],
    }),
    saveContentCard: vi.fn(),
    deleteContentCard: vi.fn(),
    uploadContentImage: vi.fn(),
}));

describe('AdminContentPage', () => {
    it('renders without crashing', async () => {
        render(<AdminContentPage />);

        await waitFor(() => {
            expect(screen.getByText(/content studio/i)).toBeInTheDocument();
        });

        expect(screen.getByTestId('card-studio-canvas')).toBeInTheDocument();
        expect(screen.getByTestId('card-studio-inspector')).toBeInTheDocument();
    });
});
