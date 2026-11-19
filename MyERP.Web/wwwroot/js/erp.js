(function () {
    "use strict";
    if (!window.angular) return;
    angular.module("erp", [])
        .controller("tableFilter", ["$scope", function ($scope) {
            $scope.query = "";
        }])
        .controller("documentLines", ["$scope", function ($scope) {
            $scope.lines = [{ itemId: "", purchaseOrderLineId: "", quantity: 1, unitPrice: 0 }];
            $scope.addLine = function () {
                $scope.lines.push({ itemId: "", purchaseOrderLineId: "", quantity: 1, unitPrice: 0 });
            };
            $scope.removeLine = function (index) {
                if ($scope.lines.length > 1) $scope.lines.splice(index, 1);
            };
        }]);
}());
