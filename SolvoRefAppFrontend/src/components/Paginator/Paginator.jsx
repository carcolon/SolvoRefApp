import React from 'react';
import './paginator.css'; // opcional si tienes estilos externos

export default function Paginator({ currentPage, totalPage, onNext, onPrev }) {
    return (
        <div className="pagination-indicator">
            <span onClick={onPrev} className="paginator">
                {'<'}
            </span>

            <span>{currentPage}</span>

            <p id="paginationLabel" className="card-title-nopad">
                OF
            </p>

            <span>{totalPage}</span>

            <span onClick={onNext} className="paginator">
                {'>'}
            </span>
        </div>
    );
}
