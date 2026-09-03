import './ViewReferidos.css';
import { referralApi } from '../../Constants/Constants';
import solamarillo from '../../assets/images/solamarillo.png';
import solzapote from '../../assets/images/solzapote.png';
import estrellla4 from '../../assets/images/estrella4.png';
import { useState, useEffect, useRef } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import DataView from '../../components/ConsumerReferidosComponent/DataViewReferal';
import FormReferidos from '../../components/ReferidoComponent/FormReferidos';
import Modal from '../../components/Modal/Modal';
import { useApi } from '../../components/CustomHook/UseApi';
import Paginator from '../../components/Paginator/Paginator';
import DetailForm from '../../components/DetailForm/DetailForm';
import Spinner from '../../components/Spinner/Spinner';
import AnimatedPageShell from '../../components/AnimatedPageShell';
import { usePageMotion } from '../../animations/usePageMotion';
import ReferralFooter from '../../components/ReferralFooter/ReferralFooter';
import { toast } from 'react-toastify';
import { FiHelpCircle } from 'react-icons/fi';
import ReferralFeatureTour from '../../components/ReferralFeatureTour/ReferralFeatureTour';

function ViewReferal() {
    const pageRef = useRef(null);
    usePageMotion(pageRef, []);

    const location = useLocation();
    const navigate = useNavigate();
    const { request } = useApi();
    const [isLoading, setIsLoading] = useState(false);
    const [newReferralModalOpen, setNewReferralModalOpen] = useState(false);
    const [rejectedCount, setRejectedCount] = useState(0);
    const [hiredCount, setHiredCont] = useState(0);
    const [inprogressCount, setInprogressCount] = useState(0);
    const [filterStatus, setFilterStatus] = useState(null);
    const [filteredReferrals, setFilteredReferrals] = useState([]);
    const [isDetailModalOpen, setIsDetailModalOpen] = useState(false);
    const [selectedReferral, setSelectedReferral] = useState(null);
    const [currentPage, setCurrentPage] = useState(1);
    const [totalPage, setTotalPage] = useState(0);
    const [isCopyingLink, setIsCopyingLink] = useState(false);
    const [tourRunSignal, setTourRunSignal] = useState(0);

    const handleFilterClick = (status) => {
        setFilterStatus((prevStatus) => (prevStatus === status ? null : status));
    };

    const getAllReferred = async () => {
        setIsLoading(true);
        const params = new URLSearchParams();
        if (filterStatus) params.append('Status', filterStatus);
        params.append('PageNumber', currentPage);
        const queryString = params.toString() ? `?${params.toString()}` : '';
        const allReferral = await request(`${process.env.REACT_APP_API}/api/${referralApi}/all/user${queryString}`, {
            method: 'GET',
            returnFullResponse: true,
        });
        if (allReferral != null) {
            const { pageNumber, totalPages, data } = allReferral;
            setFilteredReferrals(data);
            setTotalPage(totalPages);
            setCurrentPage(pageNumber);
        }
        setIsLoading(false);
    };

    const getAllReferredStatus = async () => {
        const allStatusReferral = await request(`${process.env.REACT_APP_API}/api/${referralApi}/all/status/user`);
        if (allStatusReferral != null) {
            setRejectedCount(allStatusReferral.statuses['Rejected'] ?? 0);
            setHiredCont(allStatusReferral.statuses['Hired'] ?? 0);
            setInprogressCount(allStatusReferral.statuses['In Progress'] ?? 0);
        }
    };

    const handleCopyReferralLink = async () => {
        setIsCopyingLink(true);
        const linkData = await request(`${process.env.REACT_APP_API}/api/${referralApi}/link`);
        setIsCopyingLink(false);

        if (!linkData?.url) {
            return;
        }

        try {
            await navigator.clipboard.writeText(linkData.url);
            toast.success('Referral link copied.');
        } catch (_) {
            toast.info(linkData.url);
        }
    };

    useEffect(() => {
        getAllReferred();
        getAllReferredStatus();
    }, [filterStatus, currentPage]);

    useEffect(() => {
        if (!location.state?.openNewReferral) {
            return;
        }

        setNewReferralModalOpen(true);
        navigate(location.pathname, { replace: true, state: null });
    }, [location.pathname, location.state, navigate]);

    return (
        <AnimatedPageShell ref={pageRef}>
            <div className="imagen referrals-page-shell">
                <Spinner isLoading={isLoading} />
                <Modal
                    showModal={isDetailModalOpen}
                    handleCloseClick={() => {
                        setIsDetailModalOpen(false);
                        setSelectedReferral(null);
                    }}
                    size="lg"
                    centered
                    backdrop="static"
                    keyboard={false}
                >
                    <DetailForm referralData={selectedReferral} />
                </Modal>
                <Modal
                    showModal={newReferralModalOpen}
                    handleCloseClick={() => {
                        setNewReferralModalOpen(!newReferralModalOpen);
                    }}
                    size="lg"
                    centered
                    backdrop="static"
                    keyboard={false}
                >
                    <FormReferidos setHandleClose={(status) => {
                        if (!status) {
                            getAllReferred();
                            setNewReferralModalOpen(status);
                        }
                    }} />
                </Modal>

                <section className="newfereral glass-panel referrals-toolbar">
                    <div className="referrals-toolbar-copy">
                        <h2 data-hero>My Referrals</h2>
                    </div>

                    <div className={`status-group gsap-card ${filterStatus === 'In Progress' ? 'active-filter getBackgroundColor1' : ''}`} data-card data-tour="referral-status">
                        <img className="solamarillo" src={solamarillo} alt="In progress" onClick={() => handleFilterClick('In Progress')} />
                        <div className="text-and-count text">
                            <p className="in_progres">In Progress</p>
                            <h4 className="count_inprogres">{inprogressCount}</h4>
                        </div>
                    </div>

                    <div className={`status-group gsap-card ${filterStatus === 'Hired' ? 'active-filter getBackgroundColor2' : ''}`} data-card>
                        <img className="solzapote" src={solzapote} alt="Hired" onClick={() => handleFilterClick('Hired')} />
                        <div className="text-and-count text">
                            <p className="hired_">Hired</p>
                            <h4 className="hired_count">{hiredCount}</h4>
                        </div>
                    </div>

                    <div className={`status-group gsap-card ${filterStatus === 'Rejected' ? 'active-filter getBackgroundColor3' : ''}`} data-card>
                        <img className="estrella4" src={estrellla4} alt="Rejected" onClick={() => handleFilterClick('Rejected')} />
                        <div className="text-and-count text">
                            <p className="rejected_">Rejected</p>
                            <h4 className="count_rejected">{rejectedCount}</h4>
                        </div>
                    </div>

                    <div className="referrals-toolbar-actions">
                        <button className="button1 gsap-button" onClick={() => setNewReferralModalOpen(true)} data-hero data-tour="new-referral">
                            New Referral
                        </button>
                        <button className="button1 gsap-button" onClick={handleCopyReferralLink} disabled={isCopyingLink} data-hero data-tour="copy-link">
                            {isCopyingLink ? 'Copying...' : 'Copy Link'}
                        </button>
                        <button
                            type="button"
                            className="referrals-tour-trigger gsap-button"
                            aria-label="Show referral link tour"
                            title="Show referral link tour"
                            onClick={() => setTourRunSignal((prev) => prev + 1)}
                            data-hero
                        >
                            <FiHelpCircle />
                        </button>
                    </div>
                </section>

                <DataView
                    filteredReferrals={filteredReferrals}
                    onViewDetails={(referral) => {
                        setSelectedReferral(referral);
                        setIsDetailModalOpen(true);
                    }}
                />

                <Paginator currentPage={currentPage} totalPage={totalPage} onNext={() => currentPage < totalPage && setCurrentPage((prev) => prev + 1)} onPrev={() => currentPage > 1 && setCurrentPage((prev) => prev - 1)} />
                <ReferralFooter />
                <ReferralFeatureTour runSignal={tourRunSignal} />
            </div>
        </AnimatedPageShell>
    );
}

export default ViewReferal;
