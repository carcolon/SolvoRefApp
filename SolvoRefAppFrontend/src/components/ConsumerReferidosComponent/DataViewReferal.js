import React from 'react';
import './DataViewReferal.css';
import recurso10 from '../../assets/images/Recurso 10.png';
import recurso66 from '../../assets/images/Recurso66.png';

function DataView({ filteredReferrals, onViewDetails }) {
    return (
        <div className="contenedor-general-referidos">
            {filteredReferrals && filteredReferrals.length > 0 ? (
                filteredReferrals.map((referral) => (
                    <div className="getReferals gsap-card" key={referral.id} data-card>
                        <div className="referidos">
                            <img className="recurso10" src={recurso10} alt="Referral item icon" />
                            <div className="referralInformationContainer">
                                <div className="referralInformationTitle">
                                    <p className="referral-name">{referral.name}</p>
                                    <p className="email">{referral.email}</p>
                                    <hr className="referral-separator" />
                                </div>

                                <div className="referralInformationContent">
                                    <div>
                                        <p className="status1">Status</p>
                                        <b>{referral.status}</b>
                                    </div>
                                    <div>
                                        <p className="area1">Area to apply</p>
                                        <b className="area">{referral.area}</b>
                                    </div>
                                    <div>
                                        <p className="date1">Creation Date</p>
                                        <b>{referral.creationDate}</b>
                                    </div>

                                    <div className="pView gsap-button" onClick={() => onViewDetails(referral)}>
                                        <img className="reurso66" src={recurso66} alt="View referral" />
                                        <p>View</p>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                ))
            ) : (
                <div className="empty-referrals glass-panel" data-reveal>
                    No referrals match the selected filter.
                </div>
            )}
        </div>
    );
}

export default DataView;
