#!/usr/bin/env python
# coding: utf-8

# ## 부정청약, 위장전입, 불법매매에 해당할 확률의 순위를 기반으로 조사대상을 반환하는 코드

# ### 1-1. 환경설정 및 데이터 로딩

# In[1]:


# 라이브러리 로드 및 환경설정 (아래 라이브러리가 모두 설치되어야함)
import numpy as np
import pandas as pd
import sklearn
from sklearn import preprocessing
from sklearn.ensemble  import RandomForestClassifier
from sklearn import metrics 
import xgboost as xgb   
from xgboost import XGBClassifier
pd.set_option('display.max_colwidth', None)
pd.set_option('display.max_columns', None)

import warnings
import warnings
warnings.filterwarnings(action='ignore')

# 자료 읽기 
DF=pd.read_csv(r'dataframe_train.csv', encoding='CP949', sep=",")
DF_test=pd.read_csv(r'dataframe_add.csv', encoding='CP949', sep=",")

#DF_add = DF[DF['부정청약판정'] == 0].sample(n=400, random_state=1004)

# 학습 세트의 경우, 실제 조사를 한 경우만 로딩
cond=DF['검사여부']==1
DF_train=DF[cond]
#DF_test = pd.concat([DF_test, DF_add])


# ### 1-2. 위험순위 및 사전설정 함수 정의

# In[2]:


# 위험순위에 대한 quantile points 설정 (1 기준. 예시: 0.5 = 상위 50% 추출)

measurements=[0.5, 0.25, 0.1]
#measurements=[0.9, 0.8, 0.7, 0.6, 0.5, 0.4, 0.3, 0.2, 0.1]


# 확률과 quantile point를 입력값으로 조사 결정을 변환하는 함수

def convert(pred_prob,m): 
    e=[]
    for h in range(0,len(pred_prob)):

        e.append(pred_prob[h][1])
    e_temp=sorted(e)
    e_temp.reverse()
    e_ind=[]
    for i in e:
        e_ind.append(e_temp.index(i)+1)    
    last_ind=len(e_ind)
    init=0
    for x in range(0,last_ind):
        dd=e[x]
        if dd == 0:
            e_ind[x]=last_ind
    point_m=int(round(len(e_ind)*m))
    pred=[]
    
    for x in range(0,last_ind):
        hh=e_ind[x]
    
        if hh<=point_m:
            pred.append(1)
        else:
            pred.append(0)

    return pred

 # 조사 시 검거 확률과 미스 확률을 반환하는 함수 (성능 테스트용)

def performance(X, Y):
    detect=[]
    miss=[]
    last_ind=len(Y)
    for i in range(0,last_ind):
        x_i=X[i]
        y_i=Y[i]

    
        if (x_i==1) & (y_i==1):
            detect.append(1)
        elif (x_i==1) & (y_i==0):
            detect.append(0)
        elif (x_i==0) & (y_i==1):
            miss.append(1)
        elif (x_i==0) & (y_i==0):
            miss.append(0)    
    prob_detect=detect.count(1)/len(detect)
    prob_miss=miss.count(1)/len(miss)       
    return [prob_detect,prob_miss]

# 자료 인코딩용 함수 정의 : 아래 함수에서 dataDF는 데이터프레임, list는 라벨화하려는 범주변수의 목록
def encode_labels(list, dataDF):                               
    for x in list:
        temp=preprocessing.LabelEncoder()
        dataDF[x]=temp.fit_transform(dataDF[x])
    return dataDF


# ### 1-3. 시행할 내용의 설정 (시행 시=1, 시행 안할 시=0)

# In[3]:


#------------- 각 flag는 어떤 test를 할 것인지 설정.  RF는 random forest, XGB는 XGBoost, Y1,Y2,Y3는 부정청약,위장전입,부정매매 test
flag_RF=1
flag_XGB=1

flag_Y1=1
flag_Y2=1
flag_Y3=1


# ### 2-1. random forest 및 XGBoost의 하이퍼 파라메터 설정

# In[4]:


#------- RF parameters----------

max_depth_Y1=10
max_depth_Y2=7
max_depth_Y3=14

min_samples_leaf_Y1=5
min_samples_leaf_Y2=3
min_samples_leaf_Y3=3

max_leaf_nodes_Y1=None
max_leaf_nodes_Y2=None
max_leaf_nodes_Y3=None

max_features_Y1=10
max_features_Y2=10
max_features_Y3=6

min_samples_split_Y1=4
min_samples_split_Y2=4
min_samples_split_Y3=4

bootstrap_Y1=True
bootstrap_Y2=True
bootstrap_Y3=True

warm_start_Y1=False
warm_start_Y2=False
warm_start_Y3=False

class_weight_Y1={0:1, 1:1}
class_weight_Y2={0:1, 1:1}
class_weight_Y3={0:1, 1:1}

#-------XGBoost parameters-------

reg_alpha_Y1 = 0.75
reg_alpha_Y2 = 0.75
reg_alpha_Y3 = 0.75

reg_lambda_Y1 = 0.5
reg_lambda_Y2 = 0.5
reg_lambda_Y3 = 0.5

gamma_Y1 = 0
gamma_Y2 = 0
gamma_Y3 = 0

booster_Y1 = 'gbtree'
booster_Y2 = 'gbtree'
booster_Y3 = 'gbtree'
#  'gbtree', 'gblinear' 'dart'

max_depth_Y1 = 10
max_depth_Y2 = 10
max_depth_Y3 = 10 

objective_Y1 = 'binary:logistic'
objective_Y2 = 'binary:logistic'
objective_Y3 = 'binary:logistic'
#'binary:logistic', 'binary:logitraw', 'binary:hinge'

learning_rate_Y1=0.75
learning_rate_Y2=0.75
learning_rate_Y3=0.75

min_child_weight_Y1=1
min_child_weight_Y2=1
min_child_weight_Y3=1

colsample_bytree_Y1=1
colsample_bytree_Y2=1
colsample_bytree_Y3=1

scale_pos_weight_Y1=1
scale_pos_weight_Y2=1
scale_pos_weight_Y3=1

subsample_Y1=1
subsample_Y2=1
subsample_Y3=1


# In[5]:


DF_train.describe()


# In[6]:

print('hello')

print(DF_test.columns)
print('end')

DF_test.describe()


# ### 2-2. 변수처리

# In[7]:


# 사용할 변수 리스트

X_list=['크기', 'ad_총공급', 'ad_성명생년전화중복', 'ad_행정변경시점',\
        '배우자', '2년청약건수', '세대원수', '분리세대원수', '폰중복횟수_부동산원', 'IP중복신청횟수_부동산원',\
        'ad_IP중복_3자리', 'ad_IP중복_4자리', 'ad_접수시간', 'ad_신청당첨거주일치여부', 'ad_부양가족수', 'ad_저축가입기간', 'ad_무주택기간',\
        'ad_청약납부회차', 'ad_청약경과기간', 'ad_총점', 'ad_주소일치여부', 'ad_변경시점2',\
        '공급금액', '연령', '세대주관계', '특일동시여부', '주소중복횟수', '가점합계', 'ad_신청유형', '기관추천종류',\
        '접수매체', '주택소유구분', '장기복무군인', '분양가상한제여부'
        ]
# 범주형 변수 선언
str_list = ['배우자','세대주관계', 'ad_신청유형', '기관추천종류', '접수매체', '주택소유구분', '장기복무군인']


# 결측치 제거 위해 테스트 대상 특정
#DF_train_selected=DF_train[X_list+['부정청약판정', '부정_위장전입', '부정_입주자저축증서매매']]
#DF_test_selected=DF_test[X_list]

# 결측치있는 표본 제거
DF_train_selected=DF_train.dropna()
DF_test_selected=DF_test.dropna()
print(len(DF_train)-len(DF_train_selected))
print(len(DF_test)-len(DF_test_selected))

# 설명변수 설정
X_train=DF_train_selected[X_list]
X_test=DF_test_selected[X_list]

# 예측변수 설정
Y1=DF_train_selected['부정청약판정']
Y2=DF_train_selected['부정_위장전입']
Y3=DF_train_selected['부정_입주자저축증서매매']


Y1_test=DF_test_selected['부정청약판정']
Y2_test=DF_test_selected['부정_위장전입']
Y3_test=DF_test_selected['부정_입주자저축증서매매']


# 범주형 변수 라벨화

X_train[ str_list ]=X_train[ str_list ].astype('str')
X_test[ str_list ]=X_test[ str_list ].astype('str')

tmp = pd.DataFrame()
for x in str_list:
        temp=preprocessing.LabelEncoder()
        tmp[x]=temp.fit_transform(X_test[x])
        print(x)
        mapping = dict(zip(temp.classes_, range(1, len(temp.classes_)+1)))
        print(mapping)
        

X_train=encode_labels(str_list, X_train)
X_test=encode_labels(str_list, X_test)



# ### 3. 기계학습예측기의 학습

# In[8]:


if flag_RF==1:
    if flag_Y1==1:
        rf1=RandomForestClassifier(n_estimators=100, random_state=11, max_depth=max_depth_Y1, min_samples_leaf=min_samples_leaf_Y1,\
                                   max_features=max_features_Y1, max_leaf_nodes=max_leaf_nodes_Y1, min_samples_split=min_samples_split_Y1,\
                                  bootstrap=bootstrap_Y1, warm_start=warm_start_Y1, class_weight=class_weight_Y1)
        rf1.fit(X_train,Y1)
        rf1_pred = rf1.predict_proba(X_test)
        #rf1_prd = rf1.predict(X_test)
        threshold = 0.1
        rf1_prd = (rf1.predict_proba(X_test)[:, 1] > threshold).astype('float')
    if flag_Y2==1:
        rf2=RandomForestClassifier(n_estimators=100, random_state=11, max_depth=max_depth_Y2, min_samples_leaf=min_samples_leaf_Y2,\
                                   max_features=max_features_Y2, max_leaf_nodes=max_leaf_nodes_Y2, min_samples_split=min_samples_split_Y2,\
                                   bootstrap=bootstrap_Y2, warm_start=warm_start_Y2, class_weight=class_weight_Y1)
        rf2.fit(X_train,Y2)
        rf2_pred=rf2.predict_proba(X_test)
        #rf2_prd = rf2.predict(X_test)
        rf2_prd = (rf2.predict_proba(X_test)[:, 1] > threshold).astype('float')
    if flag_Y3==1:
        rf3=RandomForestClassifier(n_estimators=100, random_state=11, max_depth=max_depth_Y3, min_samples_leaf=min_samples_leaf_Y3,\
                                   max_features=max_features_Y3, max_leaf_nodes=max_leaf_nodes_Y3, min_samples_split=min_samples_split_Y3,\
                                   bootstrap=bootstrap_Y3, warm_start=warm_start_Y3, class_weight=class_weight_Y1)
        rf3.fit(X_train,Y3)
        rf3_pred=rf3.predict_proba(X_test)
        #rf3_prd = rf3.predict(X_test)
        rf3_prd = (rf3.predict_proba(X_test)[:, 1] > threshold).astype('float')
            
#-------------------------------XGB: training-----------------------------------
if flag_XGB==1:
    if flag_Y1==1:
        xgb1=XGBClassifier(reg_alpha=reg_alpha_Y1, reg_lambda=reg_lambda_Y1, gamma=gamma_Y1, booster=booster_Y1, max_depth=max_depth_Y1,\
                           objective=objective_Y1, learning_rate=learning_rate_Y1, min_child_weight=min_child_weight_Y1,\
                           colsample_bytree=colsample_bytree_Y1, scale_pos_weight=scale_pos_weight_Y1, subsample_=subsample_Y1,\
                           verbosity = 0)
        xgb1.fit(X_train,Y1)
        xgb1_pred=xgb1.predict_proba(X_test)
        xgb1_prd = xgb1.predict(X_test)
    if flag_Y2==1:
        xgb2=XGBClassifier(reg_alpha=reg_alpha_Y2,reg_lambda=reg_lambda_Y2, gamma=gamma_Y2, booster=booster_Y2, max_depth=max_depth_Y2,\
                           objective=objective_Y2, learning_rate=learning_rate_Y2, min_child_weight=min_child_weight_Y2,\
                           colsample_bytree=colsample_bytree_Y2, scale_pos_weight=scale_pos_weight_Y2, subsample_=subsample_Y2,\
                           verbosity = 0)
        xgb2.fit(X_train,Y2)
        xgb2_pred=xgb2.predict_proba(X_test)
        xgb2_prd = xgb2.predict(X_test)
    if flag_Y3==1:
        xgb3=XGBClassifier(reg_alpha=reg_alpha_Y3,reg_lambda=reg_lambda_Y3, gamma=gamma_Y3, booster=booster_Y3, max_depth=max_depth_Y3,\
                           objective=objective_Y3, learning_rate=learning_rate_Y3, min_child_weight=min_child_weight_Y3,\
                           colsample_bytree=colsample_bytree_Y3, scale_pos_weight=scale_pos_weight_Y3, subsample_=subsample_Y3,\
                           verbosity = 0)
        xgb3.fit(X_train,Y3)
        xgb3_pred=xgb3.predict_proba(X_test)
        xgb3_prd = xgb3.predict(X_test)


# ### 4. 결과 반환

# In[9]:


if flag_Y1==1:
    
    if flag_RF==1:
        DF_test_selected['부정청약_rf_prob']=rf1_pred[:,1]
        for m in measurements:
            DF_test_selected['부정청약_rf_'+str(m)]=convert(rf1_pred,m)
            print('RF-부정청약여부/quantile=',m)
                
    if flag_XGB==1:
        DF_test_selected['부정청약_xgb_prob']=xgb1_pred[:,1]
        for m in measurements:
            DF_test_selected['부정청약_xgb_'+str(m)]=convert(xgb1_pred,m)
            print('XGB-부정청약여부/quantile=',m)
        
if flag_Y2==1:
    print('-------------------------------------위장전입여부----------------------------------')
    
    if flag_RF==1:
        DF_test_selected['위장전입_rf_prob']=rf2_pred[:,1]
        for m in measurements:
            DF_test_selected['위장전입_rf_'+str(m)]=convert(rf2_pred,m)
            print('RF-위장전입/quantile=',m)
                
    if flag_XGB==1:
        DF_test_selected['위장전입_xgb_prob']=xgb2_pred[:,1]
        for m in measurements:
            DF_test_selected['위장전입_xgb_'+str(m)]=convert(xgb2_pred,m)
            print('XGB-위장전입/quantile=',m)

    
if flag_Y3==1:
    print('-------------------------------------매매여부-------------------------------------')    
    
    if flag_RF==1:
        DF_test_selected['매매_rf_prob']=rf3_pred[:,1]
        for m in measurements:
            DF_test_selected['매매_rf_'+str(m)]=convert(rf3_pred,m)
            print('RF-불법매매/quantile=',m)
                
    if flag_XGB==1:
        DF_test_selected['매매_xgb_prob']=xgb3_pred[:,1]
        for m in measurements:
            DF_test_selected['매매_xgb_'+str(m)]=convert(xgb3_pred,m)
            print('XGB-불법매매/quantile=',m)

DF_test_selected.to_csv('Result.csv',encoding='CP949',sep=",")

# 필요 없는 순번 열 삭제를 위한 옵션 추가
# DF_test_selected.to_csv('Result.csv',encoding='CP949',sep=",", index=False)


# In[ ]:




