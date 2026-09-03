import { useEffect, useMemo, useRef, useState } from 'react';
import { useApi } from '../../components/CustomHook/UseApi';
import Modal from '../../components/Modal/Modal';
import FormReferidos from '../../components/ReferidoComponent/FormReferidos';
import Spinner from '../../components/Spinner/Spinner';
import AnimatedPageShell from '../../components/AnimatedPageShell';
import { usePageMotion } from '../../animations/usePageMotion';
import './activePositionPage.css';
import estrella from '../../assets/images/estrella2.png';
import locationIcon from '../../assets/images/location.png';
import { referralApi } from '../../Constants/Constants';
import { FiChevronLeft, FiChevronRight, FiSearch } from 'react-icons/fi';
import ReferralFooter from '../../components/ReferralFooter/ReferralFooter';

const PAGE_SIZE = 12;

const isPlaceholder = (value) => {
    const normalized = String(value || '')
        .trim()
        .toLowerCase();

    return !normalized || /^x+$/.test(normalized) || normalized === 'n/a' || normalized === 'na';
};

const normalizePosition = (position) => {
    const vacancyId = String(position?.vacancyId || '').trim();
    const positionName = String(position?.positionName || '').trim();
    const country = String(position?.country || '').trim();

    const safeTitle = isPlaceholder(positionName)
        ? (isPlaceholder(vacancyId) ? 'Position pending sync' : `Position ${vacancyId}`)
        : positionName;

    const safeCountry = isPlaceholder(country) ? 'Country pending sync' : country;

    return {
        ...position,
        vacancyId,
        positionName: safeTitle,
        country: safeCountry,
        hasPlaceholderData: isPlaceholder(positionName) || isPlaceholder(country),
    };
};

const ActivePositionPage = () => {
    const pageRef = useRef(null);
    const { request } = useApi();
    const [positions, setPositions] = useState([]);
    const [isLoading, setIsLoading] = useState(false);
    const [selectedVacancy, setSelectedVacancy] = useState(null);
    const [showModal, setShowModal] = useState(false);
    const [searchTerm, setSearchTerm] = useState('');
    const [currentPage, setCurrentPage] = useState(1);

    usePageMotion(pageRef, [positions.length, showModal]);

    const getActivePositions = async () => {
        setIsLoading(true);
        const response = await request(`${process.env.REACT_APP_API}/api/${referralApi}/vacancies/active`);
        if (response != null) {
            setPositions(Array.isArray(response) ? response.map(normalizePosition) : []);
        }
        setIsLoading(false);
    };

    useEffect(() => {
        getActivePositions();
    }, []);

    const handleAddReferral = (vacancy) => {
        setSelectedVacancy(vacancy);
        setShowModal(true);
    };

    const filteredPositions = useMemo(() => {
        const term = searchTerm.trim().toLowerCase();
        if (!term) {
            return positions;
        }

        return positions.filter((pos) =>
            [pos.positionName, pos.country, pos.vacancyId]
                .filter(Boolean)
                .some((value) => value.toLowerCase().includes(term)));
    }, [positions, searchTerm]);

    const totalPages = Math.max(1, Math.ceil(filteredPositions.length / PAGE_SIZE));

    useEffect(() => {
        setCurrentPage(1);
    }, [searchTerm, positions.length]);

    useEffect(() => {
        if (currentPage > totalPages) {
            setCurrentPage(totalPages);
        }
    }, [currentPage, totalPages]);

    const pagedPositions = useMemo(() => {
        const start = (currentPage - 1) * PAGE_SIZE;
        return filteredPositions.slice(start, start + PAGE_SIZE);
    }, [filteredPositions, currentPage]);

    return (
        <AnimatedPageShell ref={pageRef}>
            <div className="positions-page imagen">
                <Spinner isLoading={isLoading} />
                <Modal showModal={showModal} handleCloseClick={() => setShowModal(false)} size="lg" centered>
                    <FormReferidos
                        setHandleClose={() => setShowModal(false)}
                        vacancyId={selectedVacancy?.vacancyId}
                        positionName={selectedVacancy?.positionName}
                        vacancyCountry={selectedVacancy?.country}
                    />
                </Modal>

                <section className="positions-hero glass-panel">
                    <div className="positions-hero-copy">
                        <p className="section-eyebrow" data-hero>
                            Open hiring demand
                        </p>
                        <h3 className="active-title" data-hero>
                            Active Positions
                        </h3>
                        <p className="section-copy positions-copy" data-hero>
                            Explore currently open roles, confirm availability and launch a referral directly from the vacancy card.
                        </p>
                    </div>
                </section>

                <section className="positions-toolbar" data-card>
                    <label className="positions-search">
                        <FiSearch />
                        <input
                            type="text"
                            value={searchTerm}
                            onChange={(event) => setSearchTerm(event.target.value)}
                            placeholder="Search by position, country or vacancy ID"
                        />
                    </label>
                    <div className="positions-toolbar-meta">
                        <span>{filteredPositions.length} result{filteredPositions.length === 1 ? '' : 's'}</span>
                        <span>Page {currentPage} of {totalPages}</span>
                    </div>
                </section>

                <div className="positions-grid">
                    {pagedPositions.map((pos, index) => (
                        <article key={pos.vacancyId} className="position-card gsap-card" data-card>
                            <div className="card-title-row">
                                <img src={estrella} alt="icon" className="card-icon" />
                                <h6 className="card-title" title={pos.positionName}>{pos.positionName}</h6>
                            </div>
                            <div className="position-card-meta">
                                <span className="position-index">#{(currentPage - 1) * PAGE_SIZE + index + 1}</span>
                                {pos.vacancyId && !isPlaceholder(pos.vacancyId) && (
                                    <span className="position-code">ID {pos.vacancyId}</span>
                                )}
                            </div>
                            <p className="location">
                                <strong>Country:</strong>
                                <span className="location-row">
                                    <img src={locationIcon} alt="location" className="location-icon" />
                                    <span>{pos.country}</span>
                                </span>
                            </p>
                            {pos.hasPlaceholderData && (
                                <p className="position-warning">
                                    This vacancy still has placeholder data from the source feed.
                                </p>
                            )}
                            <button className="btn-referral gsap-button" onClick={() => handleAddReferral(pos)}>
                                <span>Add Referral</span>
                                <FiChevronRight />
                            </button>
                        </article>
                    ))}
                </div>

                {!isLoading && filteredPositions.length > PAGE_SIZE && (
                    <div className="positions-pagination">
                        <button
                            type="button"
                            className="positions-page-button"
                            disabled={currentPage === 1}
                            onClick={() => setCurrentPage((page) => Math.max(1, page - 1))}
                        >
                            <FiChevronLeft />
                            <span>Previous</span>
                        </button>
                        <div className="positions-page-indicator">
                            <strong>{currentPage}</strong>
                            <span>/ {totalPages}</span>
                        </div>
                        <button
                            type="button"
                            className="positions-page-button"
                            disabled={currentPage === totalPages}
                            onClick={() => setCurrentPage((page) => Math.min(totalPages, page + 1))}
                        >
                            <span>Next</span>
                            <FiChevronRight />
                        </button>
                    </div>
                )}

                {!isLoading && filteredPositions.length === 0 && (
                    <div className="positions-empty glass-panel" data-reveal>
                        No vacancies matched your search.
                    </div>
                )}
                <ReferralFooter />
            </div>
        </AnimatedPageShell>
    );
};

export default ActivePositionPage;
