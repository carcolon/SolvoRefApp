import estrella from '../assets/images/estrella2.png';
import BlueStar from '../assets/images/BlueStar.png';
import successCardIcon from '../assets/home-modals/success/success-card-icon.svg';
import flowReferCandidate from '../assets/images/flow-refer-candidate.svg';
import flowCandidateAdvance from '../assets/images/flow-candidate-advance.svg';
import flowGetHired from '../assets/images/flow-get-hired.svg';
import flowReceiveIncentive from '../assets/images/flow-receive-incentive.svg';
import { resolveContentAssetUrl } from '../config/api';

export const CONTENT_SECTIONS = {
    spotlight: 'spotlight',
    programNews: 'program_news',
};

export const BADGE_VARIANTS = [
    { value: 'default', label: 'Gray' },
    { value: 'mint', label: 'Green' },
    { value: 'teal', label: 'Blue' },
    { value: 'orange', label: 'Orange' },
    { value: 'update', label: 'Yellow' },
    { value: 'testimony', label: 'Red' },
    { value: 'campaign', label: 'Purple' },
    { value: 'custom', label: 'Custom' },
];

export const ACTION_TYPES = [
    { value: 'none', label: 'No action' },
    { value: 'modal', label: 'Open prebuilt pop-up' },
    { value: 'detail', label: 'Open this card own pop-up' },
    { value: 'route', label: 'Go to another page in the app' },
    { value: 'url', label: 'Open external website' },
];

export const ICON_OPTIONS = [
    { value: '', label: 'No icon' },
    { value: 'uploaded-image', label: 'Use uploaded image as logo' },
    { value: 'spotlight-incentive', label: 'Spotlight incentive' },
    { value: 'spotlight-positions', label: 'Spotlight positions' },
    { value: 'spotlight-success', label: 'Spotlight success' },
    { value: 'flow-refer-candidate', label: 'Flow refer candidate' },
    { value: 'flow-candidate-advance', label: 'Flow candidate advance' },
    { value: 'flow-get-hired', label: 'Flow get hired' },
    { value: 'flow-receive-incentive', label: 'Flow receive incentive' },
];

export const CONTENT_ICON_MAP = {
    'spotlight-incentive': estrella,
    'incentive-star': estrella,
    'spotlight-positions': BlueStar,
    'positions-star': BlueStar,
    'spotlight-success': successCardIcon,
    'success-card-icon': successCardIcon,
    'flow-refer-candidate': flowReferCandidate,
    'flow-candidate-advance': flowCandidateAdvance,
    'flow-get-hired': flowGetHired,
    'flow-receive-incentive': flowReceiveIncentive,
};

export const DEFAULT_HOME_CONTENT_CARDS = [
    {
        section: CONTENT_SECTIONS.spotlight,
        badgeText: '',
        badgeVariant: 'mint',
        title: 'New referral incentive',
        descriptionHtml: '<p>Get up to $500 in rewards for each talent referred in January.</p>',
        dateText: '',
        buttonText: 'More information',
        actionType: 'modal',
        actionValue: 'incentive',
        iconKey: 'spotlight-incentive',
        imageUrl: '',
        detailTitle: 'New referral incentive',
        detailContentHtml: '<p>Get up to $500 in rewards for each talent referred in January.</p>',
        displayOrder: 1,
        isPublished: true,
        publishStartUtc: '',
        publishEndUtc: '',
    },
    {
        section: CONTENT_SECTIONS.spotlight,
        badgeText: '',
        badgeVariant: 'teal',
        title: "Check out this week's open positions",
        descriptionHtml: '<p>Discover the new open positions and refer.</p>',
        dateText: '',
        buttonText: 'View Positions',
        actionType: 'route',
        actionValue: '/ActivePosition',
        iconKey: 'spotlight-positions',
        imageUrl: '',
        detailTitle: "Check out this week's open positions",
        detailContentHtml: '<p>Discover the new open positions and refer.</p>',
        displayOrder: 2,
        isPublished: true,
        publishStartUtc: '',
        publishEndUtc: '',
    },
    {
        section: CONTENT_SECTIONS.spotlight,
        badgeText: '',
        badgeVariant: 'orange',
        title: 'Stories of Soulvers who have already won',
        descriptionHtml: '<p>Discover the testimonials of colleagues who have already received their reward.</p>',
        dateText: '',
        buttonText: 'Read Stories',
        actionType: 'modal',
        actionValue: 'success',
        iconKey: 'spotlight-success',
        imageUrl: '',
        detailTitle: 'Stories of Soulvers who have already won',
        detailContentHtml: '<p>Discover the testimonials of colleagues who have already received their reward.</p>',
        displayOrder: 3,
        isPublished: true,
        publishStartUtc: '',
        publishEndUtc: '',
    },
    {
        section: CONTENT_SECTIONS.programNews,
        badgeText: 'Update',
        badgeVariant: 'update',
        title: 'New incentive policy for 2026',
        descriptionHtml: '<p>Review the latest changes to the referral incentive policy and eligibility rules.</p>',
        dateText: 'February 5, 2026',
        buttonText: 'Read More',
        actionType: 'modal',
        actionValue: 'update',
        iconKey: '',
        imageUrl: '',
        detailTitle: 'New incentive policy for 2026',
        detailContentHtml: '<p>Review the latest changes to the referral incentive policy and eligibility rules.</p>',
        displayOrder: 1,
        isPublished: true,
        publishStartUtc: '',
        publishEndUtc: '',
    },
    {
        section: CONTENT_SECTIONS.programNews,
        badgeText: 'Community',
        badgeVariant: 'testimony',
        title: 'Community Story',
        descriptionHtml: '<p>Discover experiences and insights shared by members of our community.</p>',
        dateText: 'February 5, 2026',
        buttonText: 'Read More',
        actionType: 'modal',
        actionValue: 'community',
        iconKey: '',
        imageUrl: '',
        detailTitle: 'Community Story',
        detailContentHtml: '<p>Discover experiences and insights shared by members of our community.</p>',
        displayOrder: 2,
        isPublished: true,
        publishStartUtc: '',
        publishEndUtc: '',
    },
    {
        section: CONTENT_SECTIONS.programNews,
        badgeText: 'Campaign',
        badgeVariant: 'campaign',
        title: 'Special Campaign',
        descriptionHtml: '<p>Discover the most urgent openings by country.</p>',
        dateText: 'February 5, 2026',
        buttonText: 'Read More',
        actionType: 'modal',
        actionValue: 'campaign',
        iconKey: '',
        imageUrl: '',
        detailTitle: 'Special Campaign',
        detailContentHtml: '<p>Discover the most urgent openings by country.</p>',
        displayOrder: 3,
        isPublished: true,
        publishStartUtc: '',
        publishEndUtc: '',
    },
];

export function getContentIcon(card) {
    if (!card) {
        return '';
    }

    if (card.iconKey === 'uploaded-image' && card.imageUrl) {
        return resolveContentAssetUrl(card.imageUrl);
    }

    return CONTENT_ICON_MAP[card.iconKey] || '';
}
