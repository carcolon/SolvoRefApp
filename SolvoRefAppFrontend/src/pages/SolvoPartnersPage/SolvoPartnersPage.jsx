import { useEffect, useMemo, useRef, useState } from 'react';
import { FiCheckCircle, FiRefreshCw, FiTrash2, FiUserPlus, FiUsers } from 'react-icons/fi';
import AnimatedPageShell from '../../components/AnimatedPageShell';
import { usePageMotion } from '../../animations/usePageMotion';
import {
    activateSolvoPartner,
    createSolvoPartner,
    deactivateSolvoPartner,
    fetchSolvoPartners,
    removeSolvoPartner,
} from '../../services/contentService';
import './SolvoPartnersPage.css';

export default function SolvoPartnersPage() {
    const pageRef = useRef(null);
    const [partners, setPartners] = useState([]);
    const [email, setEmail] = useState('');
    const [feedback, setFeedback] = useState('');
    const [loading, setLoading] = useState(false);
    const [saving, setSaving] = useState(false);

    usePageMotion(pageRef, [partners.length, loading]);

    const activeCount = useMemo(
        () => partners.filter((partner) => partner.isActive).length,
        [partners],
    );

    useEffect(() => {
        loadPartners();
    }, []);

    async function loadPartners() {
        setLoading(true);
        setFeedback('');
        try {
            const response = await fetchSolvoPartners();
            if (!response?.success) {
                setFeedback(response?.errors?.[0] || 'Could not load Solvo Partners.');
                return;
            }

            setPartners(Array.isArray(response.data) ? response.data : []);
        } catch {
            setFeedback('Could not load Solvo Partners.');
        } finally {
            setLoading(false);
        }
    }

    async function handleCreate(event) {
        event.preventDefault();
        const normalizedEmail = email.trim();
        if (!normalizedEmail) {
            return;
        }

        setSaving(true);
        setFeedback('');
        try {
            const response = await createSolvoPartner(normalizedEmail);
            if (!response?.success) {
                setFeedback(response?.errors?.[0] || 'Could not save Solvo Partner.');
                return;
            }

            setEmail('');
            setFeedback('Solvo Partner saved.');
            await loadPartners();
        } catch {
            setFeedback('Could not save Solvo Partner.');
        } finally {
            setSaving(false);
        }
    }

    async function handleAction(action, successMessage) {
        setSaving(true);
        setFeedback('');
        try {
            const response = await action();
            if (!response?.success) {
                setFeedback(response?.errors?.[0] || 'Could not update Solvo Partner.');
                return;
            }

            setFeedback(successMessage);
            await loadPartners();
        } catch {
            setFeedback('Could not update Solvo Partner.');
        } finally {
            setSaving(false);
        }
    }

    return (
        <AnimatedPageShell ref={pageRef}>
            <main className="solvo-partners-page">
                <section className="solvo-partners-hero" data-hero>
                    <div className="solvo-partners-hero__copy">
                        <p className="solvo-partners-kicker">Solvo Partners</p>
                        <h1>Partner program access</h1>
                        <p>
                            Manage who receives the Solvo Partner marker used in the app badge and referral
                            DataSourcing metadata.
                        </p>
                    </div>
                    <div className="solvo-partners-hero__stats" aria-label="Solvo Partner totals">
                        <FiUsers />
                        <strong>{activeCount}</strong>
                        <span>Active Partners</span>
                    </div>
                </section>

                <form className="solvo-partners-create" onSubmit={handleCreate} data-reveal>
                    <label htmlFor="solvo-partner-email">Add Solvo Partner by email</label>
                    <div className="solvo-partners-create__row">
                        <input
                            id="solvo-partner-email"
                            type="email"
                            value={email}
                            onChange={(event) => setEmail(event.target.value)}
                            placeholder="name@solvoglobal.com"
                            autoComplete="off"
                        />
                        <button type="submit" className="solvo-partners-primary-btn" disabled={saving || !email.trim()}>
                            <FiUserPlus /> {saving ? 'Saving...' : 'Add Partner'}
                        </button>
                    </div>
                </form>

                <div className="solvo-partners-toolbar" data-reveal>
                    {feedback ? <div className="solvo-partners-feedback">{feedback}</div> : <span />}
                    <button type="button" className="solvo-partners-secondary-btn" onClick={loadPartners} disabled={loading}>
                        <FiRefreshCw /> Refresh
                    </button>
                </div>

                <section className="solvo-partners-list" aria-label="Solvo Partners list">
                    {loading ? (
                        <div className="solvo-partners-empty" data-card>Loading Solvo Partners...</div>
                    ) : partners.length ? (
                        partners.map((partner) => (
                            <article key={partner.id} className={`solvo-partners-card ${partner.isActive ? '' : 'is-inactive'}`} data-card>
                                <div className="solvo-partners-card__badge" aria-hidden="true">
                                    <FiCheckCircle />
                                </div>
                                <div className="solvo-partners-card__identity">
                                    <strong>{partner.fullName || partner.email}</strong>
                                    <span>{partner.email}</span>
                                </div>
                                <span className={`solvo-partners-status ${partner.isActive ? 'is-active' : 'is-inactive'}`}>
                                    {partner.isActive ? 'Active Partner' : 'Inactive Partner'}
                                </span>
                                <div className="solvo-partners-card__actions">
                                    {partner.isActive ? (
                                        <button
                                            type="button"
                                            className="solvo-partners-secondary-btn"
                                            onClick={() => handleAction(() => deactivateSolvoPartner(partner.id), 'Solvo Partner deactivated.')}
                                            disabled={saving}
                                        >
                                            Deactivate
                                        </button>
                                    ) : (
                                        <button
                                            type="button"
                                            className="solvo-partners-secondary-btn"
                                            onClick={() => handleAction(() => activateSolvoPartner(partner.id), 'Solvo Partner activated.')}
                                            disabled={saving}
                                        >
                                            Activate
                                        </button>
                                    )}
                                    <button
                                        type="button"
                                        className="solvo-partners-secondary-btn solvo-partners-danger-btn"
                                        onClick={() => handleAction(() => removeSolvoPartner(partner.id), 'Solvo Partner removed.')}
                                        disabled={saving}
                                    >
                                        <FiTrash2 /> Remove
                                    </button>
                                </div>
                            </article>
                        ))
                    ) : (
                        <div className="solvo-partners-empty" data-card>No Solvo Partners found.</div>
                    )}
                </section>
            </main>
        </AnimatedPageShell>
    );
}
