#include "stdafx.h"
#include "named-var-set.h"
#include "noise.h"
#include "vfa-actor.h"
#include "vfa.h"
#include "policy-learner.h"
#include "actor.h"
#include "globals.h"
#include "parameters.h"
#include "logger.h"


CLASS_CONSTRUCTOR(CPolicyLearner)(CParameters* pParameters) : CParamObject(pParameters)
{
	CHILD_CLASS_FACTORY(m_pPolicy, "Policy", CDeterministicPolicy, pParameters->getChild("Deterministic-VFA-Policy"));
	//m_pPolicy = CDeterministicPolicy::getInstance(pParameters->getChild("Deterministic-VFA-Policy"));
	END_CLASS();
}

CPolicyLearner::~CPolicyLearner()
{
	delete m_pPolicy;
}

CPolicyLearner* CLASS_FACTORY(CPolicyLearner)(CParameters* pParameters)
{
	CParameters* pChild = pParameters->getChild();
	const char* type = pChild->getName();

	CHOICE("Learner-Type");
	CHOICE_ELEMENT(type, "CACLA", CCACLALearner, pParameters);
	CHOICE_ELEMENT(type, "Regular-Gradient", CRegularPolicyGradientLearner, pParameters);
	//CHOICE-ELEMENT-COMMENTED(type, "Incremental-Natural-Actor", CIncrementalNaturalActor, pParameters);
	END_CHOICE();

	END_CLASS();
	return 0;
}




/*

//doesn't work, not sure if it should either

double CVFAActor::getProbability(CState* s, CAction* a)
{
	double actionProb = 1.0;
	double actionDist = 0.0;
	double mahalanobisDist = 0.0;
	double varProd = 1.0;
	double noiseWidth;
	double var_i; //action's i-th dimension's variance
	double output;

	if (RLSimion::g_pExperiment->isEvaluationEpisode())
		return 1.0;
	//http://en.wikipedia.org/wiki/Multivariate_normal_distribution

	//if there is no correlation:
	//f(x,mu)= exp(-0.5 * (x-mu)^T * (x-mu)/var(x))/ sqrt(2*pi^k* prod var(x))

	for (int i = 0; i<m_numOutputs; i++)
	{
		output = m_pPolicyLearners[i]->getPolicy()->getVFA()->getValue(s, a);

		var_i = std::max(0.000000001, m_pPolicyLearners[i]->getPolicy()->getExpNoise()->getSigma());

		noiseWidth = fabs((a->getValue(i) - output) 
			/ a->getRangeHalfWidth(m_pPolicyLearners[i]->getPolicy()->getOutputActionIndex()));

		if (noiseWidth != 0.0)
		{
			//actionDist= (m_pExpNoise[i]->getLastValue()/noiseWidth)*3.0; //because we are using sigma=1 /3 to map values into [-1,1]
			mahalanobisDist += noiseWidth*noiseWidth / (var_i*var_i);// Variance=1.0 , otherwise it should be  / (var_i*var_i);
			varProd = varProd* var_i*var_i;
		}
	}

	if (mahalanobisDist == 0.0) return 1.0;

	double root = sqrt(pow((2 * M_PI), (double)m_numOutputs)*varProd);
	double expo = exp(-0.5*mahalanobisDist);

	return std::min(1.0, expo / root);
}
*/
