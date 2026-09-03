export const HOME_PROMO_BUILTINS = {
    incentive: {
        label: 'Referral incentive pop-up',
        previewWidth: 940,
        title: 'Your participation always pays off',
        detailTitle: 'Your participation always pays off',
        detailContentHtml: [
            '<p>For every referred candidate who gets hired, you receive USD 100 for helping grow our Wolfpack.</p>',
            '<p>And this month, we are taking it to the next level with <strong>Squad Goals</strong>: build your team (2 to 4 people per country) and compete to be the squad with the most successful referrals.</p>',
            '<p>The prize is a pizza party to celebrate your team&apos;s success. Terms and conditions apply.</p>',
        ].join(''),
    },
    success: {
        label: 'Success stories pop-up',
        previewWidth: 1180,
        title: 'Success stories & testimonials',
        detailTitle: 'Success stories & testimonials',
        detailContentHtml: [
            '<p>Meet the colleagues who have already received rewards through the Referral Program.</p>',
            '<p>This pop-up highlights recent winners, campaign moments and recognition stories from different cities.</p>',
        ].join(''),
    },
    update: {
        label: 'Program update pop-up',
        previewWidth: 940,
        title: 'Program update',
        detailTitle: 'New incentive policy for 2026',
        detailContentHtml: [
            '<p>Review the latest changes to the referral incentive policy and eligibility rules.</p>',
            '<p>Take a moment to review the program guidelines and payment process so you can make the most of every incentive available.</p>',
        ].join(''),
    },
    community: {
        label: 'Community story pop-up',
        previewWidth: 860,
        title: 'Community story',
        detailTitle: 'Community story',
        detailContentHtml: [
            '<p>&ldquo;I honestly didn&apos;t expect it. I wasn&apos;t counting on this extra income. Thanks to the Referral Program!&rdquo;</p>',
            '<p><strong>Esteban L</strong>, ganador de nuestro sorteo de USD 1,000 por referir el año pasado.</p>',
        ].join(''),
    },
    campaign: {
        label: 'Campaign pop-up',
        previewWidth: 900,
        title: 'Special campaign',
        detailTitle: 'Special campaign',
        detailContentHtml: [
            '<p>Here you will find priority roles that need to be filled quickly, representing a great opportunity to refer with higher impact.</p>',
            '<p>Some of these positions may include additional incentives or special benefits due to their urgency.</p>',
        ].join(''),
    },
};

export function getHomePromoBuiltin(modalKey) {
    return HOME_PROMO_BUILTINS[modalKey] || null;
}

export function createEditableDetailFromBuiltin(modalKey, fallbackTitle = '') {
    const config = getHomePromoBuiltin(modalKey);
    if (!config) {
        return {
            detailTitle: fallbackTitle || '',
            detailContentHtml: '',
        };
    }

    return {
        detailTitle: config.detailTitle || config.title || fallbackTitle || '',
        detailContentHtml: config.detailContentHtml || '',
    };
}
