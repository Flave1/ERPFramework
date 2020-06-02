﻿using GODPAPIs.Contracts.RequestResponse.Supplier;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace GODPAPIs.Contracts.Queries
{


    public class GetAllSupplierBusinessOwnerQuery : IRequest<SupplierBuisnessOwnerRespObj> { }

    public class GetSupplierBusinessOwnerQuery : IRequest<SupplierBuisnessOwnerRespObj>
    {
        public GetSupplierBusinessOwnerQuery() { }
        public int SupplierBusinessOwnerId { get; set; }
        public GetSupplierBusinessOwnerQuery(int supplierBusinessOwnerId)
        {
            SupplierBusinessOwnerId = supplierBusinessOwnerId;
        }
    }
     
}
